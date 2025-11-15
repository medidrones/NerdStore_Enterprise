using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NSE.Core.Mediator;
using NSE.Pedidos.API.Application.Commands;
using NSE.Pedidos.API.Application.DTO;
using NSE.Pedidos.API.Application.Queries;
using NSE.Pedidos.API.Controllers;
using NSE.WebAPI.Core.Usuario;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Pedidos.API.Tests.Controllers
{
    public class PedidoControllerTests
    {
        private readonly Mock<IMediatorHandler> _mediatorMock;
        private readonly Mock<IAspNetUser> _userMock;
        private readonly Mock<IPedidoQueries> _queriesMock;

        private readonly PedidoController _controller;

        public PedidoControllerTests()
        {
            _mediatorMock = new Mock<IMediatorHandler>();
            _userMock = new Mock<IAspNetUser>();
            _queriesMock = new Mock<IPedidoQueries>();

            _userMock.Setup(x => x.ObterUserId()).Returns(Guid.NewGuid());

            _controller = new PedidoController(
                _mediatorMock.Object,
                _userMock.Object,
                _queriesMock.Object
            );
        }

        // --------------------------------------------------------------
        // AdicionarPedido
        // --------------------------------------------------------------
        [Fact]
        public async Task AdicionarPedido_DeveChamarMediatorERetornarCustomResponse()
        {
            // Arrange
            var command = new AdicionarPedidoCommand();
            var validationResult = new ValidationResult(); // sem erros → 200 OK

            _mediatorMock
                .Setup(x => x.EnviarComando(It.IsAny<AdicionarPedidoCommand>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _controller.AdicionarPedido(command);

            // Assert
            _userMock.Verify(x => x.ObterUserId(), Times.Once);
            _mediatorMock.Verify(x => x.EnviarComando(It.IsAny<AdicionarPedidoCommand>()), Times.Once);

            Assert.IsType<OkObjectResult>(result);
        }

        // --------------------------------------------------------------
        // UltimoPedido
        // --------------------------------------------------------------

        [Fact]
        public async Task UltimoPedido_QuandoNaoExiste_DeveRetornarNotFound()
        {
            // Arrange
            _queriesMock
                .Setup(x => x.ObterUltimoPedido(It.IsAny<Guid>()))
                .Returns(Task.FromResult<PedidoDTO>(null));

            // Act
            var result = await _controller.UltimoPedido();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UltimoPedido_QuandoExiste_DeveRetornarCustomResponse()
        {
            // Arrange
            var pedidoFake = new PedidoDTO { Id = Guid.NewGuid() };

            _queriesMock
                .Setup(x => x.ObterUltimoPedido(It.IsAny<Guid>()))
                .ReturnsAsync(pedidoFake);

            // Act
            var result = await _controller.UltimoPedido();

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        // --------------------------------------------------------------
        // ListaPorCliente
        // --------------------------------------------------------------
        [Fact]
        public async Task ListaPorCliente_QuandoNaoExiste_DeveRetornarNotFound()
        {
            // Arrange
            _queriesMock
                .Setup(x => x.ObterListaPorClienteId(It.IsAny<Guid>()))
                .ReturnsAsync((IEnumerable<PedidoDTO>)null);

            // Act
            var result = await _controller.ListaPorCliente();

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ListaPorCliente_QuandoExiste_DeveRetornarCustomResponse()
        {
            // Arrange
            var pedidosFake = new List<PedidoDTO>
            {
                new PedidoDTO { Id = Guid.NewGuid() }
            };

            _queriesMock
                .Setup(x => x.ObterListaPorClienteId(It.IsAny<Guid>()))
                .ReturnsAsync(pedidosFake);

            // Act
            var result = await _controller.ListaPorCliente();

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
