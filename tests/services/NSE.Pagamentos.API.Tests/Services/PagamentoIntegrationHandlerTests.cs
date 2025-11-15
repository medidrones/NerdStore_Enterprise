using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSE.Core.DomainObjects;
using NSE.Core.Messages.Integration;
using NSE.MessageBus;
using NSE.Pagamentos.API.Models;
using NSE.Pagamentos.API.Services;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Pagamentos.API.Tests.Services
{
    public class PagamentoIntegrationHandlerTests
    {
        private readonly Mock<IMessageBus> _messageBusMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IPagamentoService> _pagamentoServiceMock;

        public PagamentoIntegrationHandlerTests()
        {
            _messageBusMock = new Mock<IMessageBus>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _serviceScopeMock = new Mock<IServiceScope>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _pagamentoServiceMock = new Mock<IPagamentoService>();

            // Simula criação de scope
            _scopeFactoryMock.Setup(x => x.CreateScope())
                .Returns(_serviceScopeMock.Object);

            _serviceScopeMock.Setup(x => x.ServiceProvider)
                .Returns(_serviceProviderMock.Object);

            _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
                .Returns(_scopeFactoryMock.Object);

            _serviceProviderMock.Setup(x => x.GetService(typeof(IPagamentoService)))
                .Returns(_pagamentoServiceMock.Object);
        }

        private PagamentoIntegrationHandler CreateHandler()
            => new PagamentoIntegrationHandler(_serviceProviderMock.Object, _messageBusMock.Object);

        // -------------------------------------------------------
        // EXECUTEASYNC deve registrar subscribers e responder
        // -------------------------------------------------------
        [Fact]
        public async Task ExecuteAsync_DeveRegistrarResponderESubscribers()
        {
            // Arrange
            var handler = CreateHandler();

            // Act
            await handler.StartAsync(CancellationToken.None);

            // Assert
            _messageBusMock.Verify(m =>
                m.RespondAsync<PedidoIniciadoIntegrationEvent, ResponseMessage>(
                    It.IsAny<Func<PedidoIniciadoIntegrationEvent, Task<ResponseMessage>>>()),
                Times.Once);

            _messageBusMock.Verify(m =>
                m.SubscribeAsync<PedidoCanceladoIntegrationEvent>(
                    "PedidoCancelado", It.IsAny<Func<PedidoCanceladoIntegrationEvent, Task>>()),
                Times.Once);

            _messageBusMock.Verify(m =>
                m.SubscribeAsync<PedidoBaixadoEstoqueIntegrationEvent>(
                    "PedidoBaixadoEstoque", It.IsAny<Func<PedidoBaixadoEstoqueIntegrationEvent, Task>>()),
                Times.Once);
        }

        // -------------------------------------------------------
        // AutorizarPagamento
        // -------------------------------------------------------
        [Fact]
        public async Task AutorizarPagamento_DeveChamarServicoERetornarResponse()
        {
            // Arrange
            var handler = CreateHandler();

            var evento = new PedidoIniciadoIntegrationEvent
            {
                PedidoId = Guid.NewGuid(),
                TipoPagamento = 1,
                Valor = 100,
                NomeCartao = "Teste",
                NumeroCartao = "1234",
                MesAnoVencimento = "12/30",
                CVV = "999"
            };

            var responseEsperada = new ResponseMessage(new FluentValidation.Results.ValidationResult());
            _pagamentoServiceMock.Setup(s => s.AutorizarPagamento(It.IsAny<Pagamento>()))
                                 .ReturnsAsync(responseEsperada);

            var metodo = typeof(PagamentoIntegrationHandler)
                .GetMethod("AutorizarPagamento", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var task = (Task<ResponseMessage>)metodo.Invoke(handler, new object[] { evento });
            var response = await task;

            // Assert
            Assert.Equal(responseEsperada, response);
            _pagamentoServiceMock.Verify(s => s.AutorizarPagamento(It.IsAny<Pagamento>()), Times.Once);
        }

        // -------------------------------------------------------
        // CancelarPagamento
        // -------------------------------------------------------
        [Fact]
        public async Task CancelarPagamento_DeveChamarServico_SemErros()
        {
            var handler = CreateHandler();

            var evento = new PedidoCanceladoIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid()
            );

            var validResult = new FluentValidation.Results.ValidationResult();
            _pagamentoServiceMock.Setup(s => s.CancelarPagamento(evento.PedidoId))
                .ReturnsAsync(new ResponseMessage(validResult));

            var metodo = typeof(PagamentoIntegrationHandler)
                .GetMethod("CancelarPagamento", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task)metodo.Invoke(handler, new object[] { evento });
            await task;

            _pagamentoServiceMock.Verify(s => s.CancelarPagamento(evento.PedidoId), Times.Once);
        }

        [Fact]
        public async Task CancelarPagamento_QuandoInvalido_DeveLancarDomainException()
        {
            var handler = CreateHandler();

            var evento = new PedidoCanceladoIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid()
            );

            var invalid = new FluentValidation.Results.ValidationResult();
            invalid.Errors.Add(new FluentValidation.Results.ValidationFailure("x", "erro"));

            _pagamentoServiceMock.Setup(s => s.CancelarPagamento(evento.PedidoId))
                .ReturnsAsync(new ResponseMessage(invalid));

            var metodo = typeof(PagamentoIntegrationHandler)
                .GetMethod("CancelarPagamento", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            await Assert.ThrowsAsync<DomainException>(() =>
            {
                var task = (Task)metodo.Invoke(handler, new object[] { evento });
                return task;
            });
        }

        // -------------------------------------------------------
        // CapturarPagamento
        // -------------------------------------------------------
        [Fact]
        public async Task CapturarPagamento_DevePublicarPedidoPago_WhenValido()
        {
            // Arrange
            var handler = CreateHandler();
            var evento = new PedidoBaixadoEstoqueIntegrationEvent(Guid.NewGuid(), Guid.NewGuid());

            var validResult = new FluentValidation.Results.ValidationResult();

            _pagamentoServiceMock.Setup(s => s.CapturarPagamento(evento.PedidoId))
                .ReturnsAsync(new ResponseMessage(validResult));

            var metodo = typeof(PagamentoIntegrationHandler)
                .GetMethod("CapturarPagamento", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act
            var task = (Task)metodo.Invoke(handler, new object[] { evento });
            await task;

            // Assert
            _pagamentoServiceMock.Verify(s => s.CapturarPagamento(evento.PedidoId), Times.Once);
            _messageBusMock.Verify(m => m.PublishAsync(
                It.IsAny<PedidoPagoIntegrationEvent>()), Times.Once);
        }

        [Fact]
        public async Task CapturarPagamento_QuandoInvalido_DeveLancarDomainException()
        {
            // Arrange
            var handler = CreateHandler();
            var evento = new PedidoBaixadoEstoqueIntegrationEvent(Guid.NewGuid(), Guid.NewGuid());

            var invalid = new FluentValidation.Results.ValidationResult();
            invalid.Errors.Add(new FluentValidation.Results.ValidationFailure("x", "erro"));

            _pagamentoServiceMock.Setup(s => s.CapturarPagamento(evento.PedidoId))
                .ReturnsAsync(new ResponseMessage(invalid));

            var metodo = typeof(PagamentoIntegrationHandler)
                .GetMethod("CapturarPagamento", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Act & Assert
            await Assert.ThrowsAsync<DomainException>(() =>
            {
                var task = (Task)metodo.Invoke(handler, new object[] { evento });
                return task;
            });
        }
    }
}
