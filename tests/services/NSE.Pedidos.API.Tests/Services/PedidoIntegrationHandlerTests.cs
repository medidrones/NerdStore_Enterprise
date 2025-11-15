using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSE.Core.Data;
using NSE.Core.DomainObjects;
using NSE.Core.Messages.Integration;
using NSE.MessageBus;
using NSE.Pedidos.API.Services;
using NSE.Pedidos.Domain.Pedidos;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Pedidos.API.Tests.Services
{
    public class PedidoIntegrationHandlerTests
    {
        private readonly Mock<IMessageBus> _busMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IPedidoRepository> _pedidoRepositoryMock;
        private readonly Mock<IUnitOfWork> _uowMock;

        private readonly PedidoIntegrationHandler _handler;

        public PedidoIntegrationHandlerTests()
        {
            _busMock = new Mock<IMessageBus>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _pedidoRepositoryMock = new Mock<IPedidoRepository>();
            _uowMock = new Mock<IUnitOfWork>();

            // Unidade de trabalho do repositório
            _pedidoRepositoryMock.Setup(r => r.UnitOfWork).Returns(_uowMock.Object);

            // Provider DO ESCOPO (crucial)
            var scopeServiceProviderMock = new Mock<IServiceProvider>();

            // Escopo devolve o provider interno
            _scopeMock
                .Setup(s => s.ServiceProvider)
                .Returns(scopeServiceProviderMock.Object);

            // A factory devolve o escopo
            _scopeFactoryMock
                .Setup(f => f.CreateScope())
                .Returns(_scopeMock.Object);

            // O provider raiz devolve a factory de escopo
            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(_scopeFactoryMock.Object);

            // Provider do escopo devolve o repositório
            scopeServiceProviderMock
                .Setup(sp => sp.GetService(typeof(IPedidoRepository)))
                .Returns(_pedidoRepositoryMock.Object);

            // Handler final
            _handler = new PedidoIntegrationHandler(_serviceProviderMock.Object, _busMock.Object);
        }

        // ------------------------------------------------------------
        // 1. Subscribers são registrados
        // ------------------------------------------------------------
        [Fact]
        public async Task ExecuteAsync_DeveRegistrarSubscribers()
        {
            await _handler.StartAsync(CancellationToken.None);

            _busMock.Verify(x =>
                x.SubscribeAsync<PedidoCanceladoIntegrationEvent>(
                    "PedidoCancelado", It.IsAny<Func<PedidoCanceladoIntegrationEvent, Task>>()),
                Times.Once);

            _busMock.Verify(x =>
                x.SubscribeAsync<PedidoPagoIntegrationEvent>(
                    "PedidoPago", It.IsAny<Func<PedidoPagoIntegrationEvent, Task>>()),
                Times.Once);
        }

        // ------------------------------------------------------------
        // 2. CancelarPedido sucesso
        // ------------------------------------------------------------
        [Fact]
        public async Task CancelarPedido_QuandoSucesso_DeveExecutarFluxoCompleto()
        {
            var pedidoId = Guid.NewGuid();
            var clienteId = Guid.NewGuid();

            var pedido = CriarPedidoValido(clienteId);

            _pedidoRepositoryMock.Setup(r => r.ObterPorId(pedidoId)).ReturnsAsync(pedido);
            _uowMock.Setup(u => u.Commit()).ReturnsAsync(true);

            var message = new PedidoCanceladoIntegrationEvent(clienteId, pedidoId);

            Func<PedidoCanceladoIntegrationEvent, Task> callback = null;

            _busMock.Setup(x =>
                x.SubscribeAsync<PedidoCanceladoIntegrationEvent>(
                    "PedidoCancelado",
                    It.IsAny<Func<PedidoCanceladoIntegrationEvent, Task>>())
            ).Callback<string, Func<PedidoCanceladoIntegrationEvent, Task>>((_, func) => callback = func);

            await _handler.StartAsync(CancellationToken.None);

            await callback(message);

            _pedidoRepositoryMock.Verify(r => r.ObterPorId(pedidoId), Times.Once);
            _pedidoRepositoryMock.Verify(r => r.Atualizar(pedido), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        // ------------------------------------------------------------
        // 3. CancelarPedido – commit falha
        // ------------------------------------------------------------
        [Fact]
        public async Task CancelarPedido_QuandoCommitFalha_DeveLancarDomainException()
        {
            var pedidoId = Guid.NewGuid();
            var clienteId = Guid.NewGuid();

            var pedido = CriarPedidoValido(clienteId);

            _pedidoRepositoryMock.Setup(r => r.ObterPorId(pedidoId)).ReturnsAsync(pedido);
            _uowMock.Setup(u => u.Commit()).ReturnsAsync(false);

            var message = new PedidoCanceladoIntegrationEvent(clienteId, pedidoId);

            Func<PedidoCanceladoIntegrationEvent, Task> callback = null;

            _busMock.Setup(x =>
                x.SubscribeAsync<PedidoCanceladoIntegrationEvent>(
                    "PedidoCancelado",
                    It.IsAny<Func<PedidoCanceladoIntegrationEvent, Task>>())
            ).Callback<string, Func<PedidoCanceladoIntegrationEvent, Task>>((_, func) => callback = func);

            await _handler.StartAsync(CancellationToken.None);

            await Assert.ThrowsAsync<DomainException>(() => callback(message));
        }

        // ------------------------------------------------------------
        // 4. FinalizarPedido sucesso
        // ------------------------------------------------------------
        [Fact]
        public async Task FinalizarPedido_QuandoSucesso_DeveExecutarFluxoCompleto()
        {
            var pedidoId = Guid.NewGuid();
            var clienteId = Guid.NewGuid();

            var pedido = CriarPedidoValido(clienteId);

            _pedidoRepositoryMock.Setup(r => r.ObterPorId(pedidoId)).ReturnsAsync(pedido);
            _uowMock.Setup(u => u.Commit()).ReturnsAsync(true);

            var message = new PedidoPagoIntegrationEvent(clienteId, pedidoId);

            Func<PedidoPagoIntegrationEvent, Task> callback = null;

            _busMock.Setup(x =>
                x.SubscribeAsync<PedidoPagoIntegrationEvent>(
                    "PedidoPago",
                    It.IsAny<Func<PedidoPagoIntegrationEvent, Task>>())
            ).Callback<string, Func<PedidoPagoIntegrationEvent, Task>>((_, func) => callback = func);

            await _handler.StartAsync(CancellationToken.None);

            await callback(message);

            _pedidoRepositoryMock.Verify(r => r.ObterPorId(pedidoId), Times.Once);
            _pedidoRepositoryMock.Verify(r => r.Atualizar(pedido), Times.Once);
            _uowMock.Verify(u => u.Commit(), Times.Once);
        }

        // ------------------------------------------------------------
        // 5. FinalizarPedido – commit falha
        // ------------------------------------------------------------
        [Fact]
        public async Task FinalizarPedido_QuandoCommitFalha_DeveLancarDomainException()
        {
            var pedidoId = Guid.NewGuid();
            var clienteId = Guid.NewGuid();

            var pedido = CriarPedidoValido(clienteId);

            _pedidoRepositoryMock.Setup(r => r.ObterPorId(pedidoId)).ReturnsAsync(pedido);
            _uowMock.Setup(u => u.Commit()).ReturnsAsync(false);

            var message = new PedidoPagoIntegrationEvent(clienteId, pedidoId);

            Func<PedidoPagoIntegrationEvent, Task> callback = null;

            _busMock.Setup(x =>
                x.SubscribeAsync<PedidoPagoIntegrationEvent>(
                    "PedidoPago",
                    It.IsAny<Func<PedidoPagoIntegrationEvent, Task>>())
            ).Callback<string, Func<PedidoPagoIntegrationEvent, Task>>((_, func) => callback = func);

            await _handler.StartAsync(CancellationToken.None);

            await Assert.ThrowsAsync<DomainException>(() => callback(message));
        }

        // ------------------------------------------------------------
        // Helper para criar pedido
        // ------------------------------------------------------------
        private Pedido CriarPedidoValido(Guid clienteId)
        {
            return new Pedido(
                clienteId,
                valorTotal: 100,
                pedidoItems: new List<PedidoItem>
                {
                    new PedidoItem(Guid.NewGuid(), "Produto Teste", 1, 100)
                },
                voucherUtilizado: false,
                desconto: 0,
                voucherId: null
            );
        }
    }
}
