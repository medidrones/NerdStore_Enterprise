using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NSE.Core.Messages.Integration;
using NSE.MessageBus;
using NSE.Pedidos.API.Application.DTO;
using NSE.Pedidos.API.Application.Queries;
using NSE.Pedidos.API.Services;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Pedidos.API.Tests.Services
{
    public class PedidoOrquestradorIntegrationHandlerTests
    {
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<ILogger<PedidoOrquestradorIntegrationHandler>> _loggerMock;
        private readonly Mock<IPedidoQueries> _pedidoQueriesMock;
        private readonly Mock<IMessageBus> _busMock;

        private PedidoOrquestradorIntegrationHandler _handler;

        public PedidoOrquestradorIntegrationHandlerTests()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _loggerMock = new Mock<ILogger<PedidoOrquestradorIntegrationHandler>>();
            _pedidoQueriesMock = new Mock<IPedidoQueries>();
            _busMock = new Mock<IMessageBus>();

            // O ESCOPO precisa devolver o ServiceProvider interno
            var scopeServiceProviderMock = new Mock<IServiceProvider>();

            // Configure o ServiceScope → ServiceProvider interno
            _scopeMock
                .Setup(s => s.ServiceProvider)
                .Returns(scopeServiceProviderMock.Object);

            // Configure o factory → escopo
            _scopeFactoryMock
                .Setup(f => f.CreateScope())
                .Returns(_scopeMock.Object);

            // Configure o provider raiz → scopeFactory
            _serviceProviderMock
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(_scopeFactoryMock.Object);

            // ESCOPO deve conseguir resolver IPedidoQueries
            scopeServiceProviderMock
                .Setup(sp => sp.GetService(typeof(IPedidoQueries)))
                .Returns(_pedidoQueriesMock.Object);

            // ESCOPO deve conseguir resolver IMessageBus
            scopeServiceProviderMock
                .Setup(sp => sp.GetService(typeof(IMessageBus)))
                .Returns(_busMock.Object);

            _handler = new PedidoOrquestradorIntegrationHandler(
                _loggerMock.Object,
                _serviceProviderMock.Object);
        }

        private async Task InvokeProcessarPedidos()
        {            
            var method = typeof(PedidoOrquestradorIntegrationHandler)
                .GetMethod("ProcessarPedidos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task)method.Invoke(_handler, new object[] { null });

            if (task != null)
                await task;
        }

        // ---------------------------------------------------------
        // Cenário 1 — Não há pedidos autorizados
        // ---------------------------------------------------------
        [Fact]
        public async Task ProcessarPedidos_SemPedidosNaoPublicaEvento()
        {
            _pedidoQueriesMock
                .Setup(q => q.ObterPedidosAutorizados())
                .ReturnsAsync((PedidoDTO)null);

            await InvokeProcessarPedidos();

            _busMock.Verify(x => x.PublishAsync(It.IsAny<PedidoAutorizadoIntegrationEvent>()), Times.Never);
        }

        // ---------------------------------------------------------
        // Cenário 2 — Há pedido autorizado → Deve publicar evento
        // ---------------------------------------------------------
        [Fact]
        public async Task ProcessarPedidos_PedidoAutorizado_DevePublicarEvento()
        {
            var pedido = new PedidoDTO
            {
                Id = Guid.NewGuid(),
                ClienteId = Guid.NewGuid(),
                PedidoItems = new List<PedidoItemDTO>
                {
                    new PedidoItemDTO
                    {
                        ProdutoId = Guid.NewGuid(),
                        Quantidade = 2
                    }
                }
            };

            _pedidoQueriesMock
                .Setup(q => q.ObterPedidosAutorizados())
                .ReturnsAsync(pedido);

            await InvokeProcessarPedidos();

            _busMock.Verify(
                x => x.PublishAsync(It.Is<PedidoAutorizadoIntegrationEvent>(
                    evt => evt.PedidoId == pedido.Id &&
                           evt.ClienteId == pedido.ClienteId)),
                Times.Once);
        }

        // ---------------------------------------------------------
        // Cenário 3 — StartAsync inicializa o Timer e registra logs
        // ---------------------------------------------------------
        [Fact]
        public async Task StartAsync_DeveLogarInicializacao()
        {
            await _handler.StartAsync(CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        // ---------------------------------------------------------
        // Cenário 4 — StopAsync para o Timer e loga chamada
        // ---------------------------------------------------------
        [Fact]
        public async Task StopAsync_DeveLogarFinalizacao()
        {
            await _handler.StartAsync(CancellationToken.None);
            await _handler.StopAsync(CancellationToken.None);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeast(2));
        }
    }
}
