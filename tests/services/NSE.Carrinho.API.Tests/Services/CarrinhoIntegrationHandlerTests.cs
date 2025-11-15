using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSE.Carrinho.API.Data;
using NSE.Carrinho.API.Services;
using NSE.Core.Messages.Integration;
using NSE.MessageBus;
using System;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Carrinho.API.Tests.Services
{
    public class CarrinhoIntegrationHandlerTests
    {
        private readonly Fixture _fixture;

        public CarrinhoIntegrationHandlerTests()
        {
            _fixture = new Fixture();
        }

        [Fact]
        public async Task Deve_Remover_Carrinho_Quando_Pedido_Realizado()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<CarrinhoContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var serviceProvider = new ServiceCollection()
                .AddScoped(_ => new CarrinhoContext(options))
                .BuildServiceProvider();

            var clienteId = Guid.NewGuid();

            // Seed inicial
            using (var scope = serviceProvider.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<CarrinhoContext>();
                ctx.CarrinhoCliente.Add(new NSE.Carrinho.API.Model.CarrinhoCliente
                {
                    ClienteId = clienteId
                });

                await ctx.SaveChangesAsync();
            }

            var messageBusMock = new Mock<IMessageBus>();

            Func<PedidoRealizadoIntegrationEvent, Task> subscriberCallback = null;

            // Mock da inscrição
            messageBusMock
                .Setup(m => m.SubscribeAsync<PedidoRealizadoIntegrationEvent>(
                    "PedidoRealizado",
                    It.IsAny<Func<PedidoRealizadoIntegrationEvent, Task>>()))
                .Callback<string, Func<PedidoRealizadoIntegrationEvent, Task>>((_, callback) =>
                {
                    subscriberCallback = callback;
                });

            var handler = new CarrinhoIntegrationHandler(serviceProvider, messageBusMock.Object);

            // Act
            await handler.StartAsync(default);

            // Simula evento recebido no bus
            var eventMessage = new PedidoRealizadoIntegrationEvent(clienteId);

            await subscriberCallback(eventMessage);

            // Assert
            using (var scope = serviceProvider.CreateScope())
            {
                var ctx = scope.ServiceProvider.GetRequiredService<CarrinhoContext>();
                var carrinho = await ctx.CarrinhoCliente.FirstOrDefaultAsync(x => x.ClienteId == clienteId);

                Assert.Null(carrinho);
            }

            messageBusMock.Verify(m => m.SubscribeAsync<PedidoRealizadoIntegrationEvent>(
                "PedidoRealizado",
                It.IsAny<Func<PedidoRealizadoIntegrationEvent, Task>>()),
                Times.Once);
        }
    }
}
