using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NSE.Clientes.API.Application.Commands;
using NSE.Clientes.API.Controllers;
using NSE.Clientes.API.Models;
using NSE.Core.Mediator;
using NSE.WebAPI.Core.Usuario;
using System;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Clientes.API.Tests.Controllers
{
    public class ClientesControllerTests
    {
        private readonly Mock<IClienteRepository> _clienteRepositoryMock;
        private readonly Mock<IMediatorHandler> _mediatorMock;
        private readonly Mock<IAspNetUser> _userMock;

        private readonly ClientesController _controller;

        public ClientesControllerTests()
        {
            _clienteRepositoryMock = new Mock<IClienteRepository>();
            _mediatorMock = new Mock<IMediatorHandler>();
            _userMock = new Mock<IAspNetUser>();

            _userMock.Setup(u => u.ObterUserId()).Returns(Guid.NewGuid());

            _controller = new ClientesController(
                _clienteRepositoryMock.Object,
                _mediatorMock.Object,
                _userMock.Object
            );
        }

        [Fact]
        public async Task ObterEndereco_DeveRetornarOk_ComEnderecoExistente()
        {
            // Arrange
            var endereco = new Endereco(
                "Rua A",
                "123",
                "00000-000",
                "Bairro Central",
                "Cidade XPTO",
                "SP",
                "12345678",
                Guid.NewGuid());

            _clienteRepositoryMock
                .Setup(r => r.ObterEnderecoPorId(It.IsAny<Guid>()))
                .ReturnsAsync(endereco);

            // Act
            var result = await _controller.ObterEndereco();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(endereco, okResult.Value);
        }

        [Fact]
        public async Task ObterEndereco_DeveRetornarNotFound_QuandoEnderecoNaoExistir()
        {
            // Arrange
            _clienteRepositoryMock
                .Setup(r => r.ObterEnderecoPorId(It.IsAny<Guid>()))
                .ReturnsAsync((Endereco)null);

            // Act
            var result = await _controller.ObterEndereco();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AdicionarEndereco_DeveEnviarComando_ComClienteIdDoUsuario()
        {
            // Arrange
            Guid userId = Guid.NewGuid();
            _userMock.Setup(u => u.ObterUserId()).Returns(userId);

            var command = new AdicionarEnderecoCommand
            {
                Logradouro = "Rua A",
                Numero = "100"
            };
            
            _mediatorMock
                .Setup(m => m.EnviarComando(It.IsAny<AdicionarEnderecoCommand>()))
                .ReturnsAsync(new ValidationResult
                {
                    Errors = { new ValidationFailure("Campo", "Erro de validação") }
                });

            // Act
            var result = await _controller.AdicionarEndereco(command);

            // Assert
            _mediatorMock.Verify(m =>
                m.EnviarComando(It.Is<AdicionarEnderecoCommand>(c => c.ClienteId == userId)),
                Times.Once);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task AdicionarEndereco_DeveRetornarBadRequest_QuandoMediatorRetornarErro()
        {
            // Arrange
            _mediatorMock
                .Setup(m => m.EnviarComando(It.IsAny<AdicionarEnderecoCommand>()))
                .ReturnsAsync(new ValidationResult
                {
                    Errors = { new ValidationFailure("Campo", "Erro de validação") }
                });

            var command = new AdicionarEnderecoCommand
            {
                Logradouro = "Rua XPTO",
                Numero = "999"
            };

            // Act
            var result = await _controller.AdicionarEndereco(command);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
