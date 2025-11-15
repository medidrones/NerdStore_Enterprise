using FluentAssertions;
using FluentValidation.Results;
using Moq;
using NSE.Core.Messages.Integration;
using NSE.MessageBus;
using NSE.Pedidos.API.Application.Commands;
using NSE.Pedidos.API.Application.DTO;
using NSE.Pedidos.Domain;
using NSE.Pedidos.Domain.Pedidos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Pedidos.API.Tests.Application.Commands
{
    public class PedidoCommandHandlerTests
    {
        private readonly Mock<IVoucherRepository> _voucherRepositoryMock;
        private readonly Mock<IPedidoRepository> _pedidoRepositoryMock;
        private readonly Mock<IMessageBus> _busMock;

        private readonly PedidoCommandHandler _handler;

        public PedidoCommandHandlerTests()
        {
            _voucherRepositoryMock = new Mock<IVoucherRepository>();
            _pedidoRepositoryMock = new Mock<IPedidoRepository>();
            _busMock = new Mock<IMessageBus>();

            _pedidoRepositoryMock.Setup(r => r.UnitOfWork.Commit())
                .ReturnsAsync(true);

            _handler = new PedidoCommandHandler(
                _voucherRepositoryMock.Object,
                _pedidoRepositoryMock.Object,
                _busMock.Object);
        }

        private AdicionarPedidoCommand CriarComandoValido(bool utilizarVoucher = false)
        {
            return new AdicionarPedidoCommand
            {
                ClienteId = Guid.NewGuid(),
                PedidoItems = new List<PedidoItemDTO>
                {
                    new PedidoItemDTO { ProdutoId = Guid.NewGuid(), Quantidade = 2, Valor = 50 }
                },
                ValorTotal = 100,
                VoucherUtilizado = utilizarVoucher,
                Desconto = 0,
                Endereco = new EnderecoDTO
                {
                    Logradouro = "Rua Teste",
                    Numero = "123",
                    Bairro = "Centro",
                    Cidade = "Cidade Teste",
                    Estado = "TS",
                    Cep = "99999999"
                },
                NomeCartao = "Teste",
                NumeroCartao = "1111222233334444",
                ExpiracaoCartao = "12/2030",
                CvvCartao = "123"
            };
        }

        // -----------------------------------------------------------
        // 1. Comando inválido
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ComandoInvalido_DeveRetornarValidationResultComErros()
        {
            var command = new AdicionarPedidoCommand
            {
                ClienteId = Guid.Empty,
                ValorTotal = 0,
                PedidoItems = new List<PedidoItemDTO>(),
                NomeCartao = "",
                NumeroCartao = "",
                ExpiracaoCartao = "",
                CvvCartao = "",
                Endereco = new EnderecoDTO()
            };

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }

        // -----------------------------------------------------------
        // 2. Voucher informado e não existe
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_VoucherUtilizadoMasNaoExiste_DeveRetornarErro()
        {
            var command = CriarComandoValido(true);
            command.VoucherCodigo = "PROMO10";

            _voucherRepositoryMock.Setup(v => v.ObterVoucherPorCodigo("PROMO10"))
                .ReturnsAsync((Voucher)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "O voucher informado não existe!");
        }

        // -----------------------------------------------------------
        // 3. Voucher inválido (falha nas regras)
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_VoucherInvalido_DeveRetornarErrosDeValidacao()
        {
            var command = CriarComandoValido(true);
            command.VoucherCodigo = "PROMO10";

            var voucher = new Voucher();

            _voucherRepositoryMock
                .Setup(v => v.ObterVoucherPorCodigo("PROMO10"))
                .ReturnsAsync(voucher);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
        }

        // -----------------------------------------------------------
        // 4. Pedido inválido (valor divergente)
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ValidacaoPedidoInvalida_DeveRetornarErro()
        {
            var command = CriarComandoValido();
            command.ValorTotal = 999; // divergência

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e =>
                e.ErrorMessage == "O valor total do pedido não confere com o cálculo do pedido");
        }

        // -----------------------------------------------------------
        // 5. Pagamento recusado
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_ProcessamentoPagamentoFalhou_DeveRetornarValidationResultComErros()
        {
            var command = CriarComandoValido();

            var response = new ResponseMessage(new ValidationResult(new List<ValidationFailure>
            {
                new ValidationFailure("Pagamento", "Cartão recusado")
            }));

            _busMock.Setup(b => b.RequestAsync<PedidoIniciadoIntegrationEvent, ResponseMessage>(
                It.IsAny<PedidoIniciadoIntegrationEvent>()))
            .ReturnsAsync(response);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "Cartão recusado");
        }

        // -----------------------------------------------------------
        // 6. Fluxo completo — caminho feliz
        // -----------------------------------------------------------
        [Fact]
        public async Task Handle_CaminhoFeliz_DevePersistirPedidoEVoucher()
        {
            var command = CriarComandoValido();

            var response = new ResponseMessage(new ValidationResult());

            _busMock.Setup(b => b.RequestAsync<PedidoIniciadoIntegrationEvent, ResponseMessage>(
                It.IsAny<PedidoIniciadoIntegrationEvent>()))
            .ReturnsAsync(response);

            var result = await _handler.Handle(command, CancellationToken.None);

            // Deve persistir pedido
            _pedidoRepositoryMock.Verify(r => r.Adicionar(It.IsAny<Pedido>()), Times.Once);

            // Deve cometer transação
            _pedidoRepositoryMock.Verify(r => r.UnitOfWork.Commit(), Times.Once);

            result.IsValid.Should().BeTrue();
        }
    }
}
