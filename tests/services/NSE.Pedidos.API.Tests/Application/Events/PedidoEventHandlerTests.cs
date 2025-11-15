using Moq;
using NSE.Core.Messages.Integration;
using NSE.MessageBus;
using NSE.Pedidos.API.Application.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Pedidos.API.Tests.Application.Events
{
    public class PedidoEventHandlerTests
    {
        private readonly Mock<IMessageBus> _busMock;
        private readonly PedidoEventHandler _handler;

        public PedidoEventHandlerTests()
        {
            _busMock = new Mock<IMessageBus>();
            _handler = new PedidoEventHandler(_busMock.Object);
        }

        [Fact]
        public async Task Handle_QuandoEventoRecebido_DevePublicarPedidoRealizadoIntegrationEvent()
        {
            // Arrange
            var clienteId = Guid.NewGuid();
            var evento = new PedidoRealizadoEvent(Guid.NewGuid(), clienteId);

            // Act
            await _handler.Handle(evento, CancellationToken.None);

            // Assert
            _busMock.Verify(
                b => b.PublishAsync(
                    It.Is<PedidoRealizadoIntegrationEvent>(e => e.ClienteId == clienteId)
                ),
                Times.Once);
        }
    }
}
