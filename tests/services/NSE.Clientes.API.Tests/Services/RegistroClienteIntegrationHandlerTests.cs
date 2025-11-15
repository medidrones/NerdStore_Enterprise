using EasyNetQ;
using FluentValidation.Results;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSE.Clientes.API.Application.Commands;
using NSE.Clientes.API.Services;
using NSE.Core.Mediator;
using NSE.Core.Messages.Integration;
using NSE.MessageBus;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Clientes.API.Tests.Services
{
    public class RegistroClienteIntegrationHandlerTests
    {
        private readonly Mock<IMessageBus> _busMock;
        private readonly Mock<IAdvancedBus> _advancedBusMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
        private readonly Mock<IServiceScope> _scopeMock;
        private readonly Mock<IMediatorHandler> _mediatorMock;

        private Func<UsuarioRegistradoIntegrationEvent, Task<ResponseMessage>> _capturedResponder;

        public RegistroClienteIntegrationHandlerTests()
        {
            _busMock = new Mock<IMessageBus>();
            _advancedBusMock = new Mock<IAdvancedBus>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _scopeFactoryMock = new Mock<IServiceScopeFactory>();
            _scopeMock = new Mock<IServiceScope>();
            _mediatorMock = new Mock<IMediatorHandler>();

            // Configura AdvancedBus
            _busMock.Setup(b => b.AdvancedBus)
                .Returns(_advancedBusMock.Object);

            // Captura responder registrado
            _busMock
                .Setup(b => b.RespondAsync<UsuarioRegistradoIntegrationEvent, ResponseMessage>(
                    It.IsAny<Func<UsuarioRegistradoIntegrationEvent, Task<ResponseMessage>>>()))
                .Callback<Func<UsuarioRegistradoIntegrationEvent, Task<ResponseMessage>>>(responder =>
                {
                    _capturedResponder = responder;
                });

            // Configura ServiceScope
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(_scopeFactoryMock.Object);

            _scopeMock.Setup(s => s.ServiceProvider)
                .Returns(_serviceProviderMock.Object);

            _scopeFactoryMock.Setup(sf => sf.CreateScope())
                .Returns(_scopeMock.Object);

            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IMediatorHandler)))
                .Returns(_mediatorMock.Object);
        }

        private RegistroClienteIntegrationHandler CreateHandler()
        {
            return new RegistroClienteIntegrationHandler(
                _serviceProviderMock.Object,
                _busMock.Object
            );
        }

        [Fact]
        public async Task ExecuteAsync_DeveRegistrarResponder()
        {
            // Arrange
            var handler = CreateHandler();

            // Act
            await handler.StartAsync(CancellationToken.None);

            // Assert
            _busMock.Verify(b =>
                b.RespondAsync<UsuarioRegistradoIntegrationEvent, ResponseMessage>(
                    It.IsAny<Func<UsuarioRegistradoIntegrationEvent, Task<ResponseMessage>>>()),
                Times.Once);
        }

        [Fact]
        public async Task OnConnect_DeveRegistrarResponderNovamente()
        {
            // Arrange
            var handler = CreateHandler();

            await handler.StartAsync(CancellationToken.None);

            // Act — simula reconexão do bus
            _advancedBusMock.Raise(ab => ab.Connected += null, EventArgs.Empty);

            // Assert — deve registrar novamente
            _busMock.Verify(b =>
                b.RespondAsync<UsuarioRegistradoIntegrationEvent, ResponseMessage>(
                    It.IsAny<Func<UsuarioRegistradoIntegrationEvent, Task<ResponseMessage>>>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task RegistrarCliente_DeveEnviarComando_E_RetornarResponseMessage()
        {
            // Arrange
            var handler = CreateHandler();
            await handler.StartAsync(CancellationToken.None);

            var message = new UsuarioRegistradoIntegrationEvent(
                Guid.NewGuid(),
                "Fulano da Silva",
                "fulano@empresa.com",
                "11111111111"
            );

            var validationResult = new ValidationResult();
            _mediatorMock
                .Setup(m => m.EnviarComando(It.IsAny<RegistrarClienteCommand>()))
                .ReturnsAsync(validationResult);

            // Act — executa o responder capturado
            var response = await _capturedResponder(message);

            // Assert
            _mediatorMock.Verify(m =>
                m.EnviarComando(It.Is<RegistrarClienteCommand>(cmd =>
                       cmd.Id == message.Id &&
                       cmd.Nome == message.Nome &&
                       cmd.Email == message.Email &&
                       cmd.Cpf == message.Cpf
                )),
                Times.Once);

            Assert.NotNull(response);
            Assert.Equal(validationResult, response.ValidationResult);
        }
    }
}
