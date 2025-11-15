using Microsoft.AspNetCore.Mvc;
using Moq;
using NSE.Pedidos.API.Application.DTO;
using NSE.Pedidos.API.Application.Queries;
using NSE.Pedidos.API.Controllers;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Pedidos.API.Tests.Controllers
{
    public class VoucherControllerTests
    {
        private readonly Mock<IVoucherQueries> _voucherQueriesMock;
        private readonly VoucherController _controller;

        public VoucherControllerTests()
        {
            _voucherQueriesMock = new Mock<IVoucherQueries>();

            _controller = new VoucherController(
                _voucherQueriesMock.Object
            );
        }

        // --------------------------------------------------------------
        // 1. Código inválido
        // --------------------------------------------------------------

        [Fact]
        public async Task ObterPorCodigo_QuandoCodigoVazio_DeveRetornarNotFound()
        {
            // Arrange
            string codigo = "";

            // Act
            var result = await _controller.ObterPorCodigo(codigo);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // --------------------------------------------------------------
        // 2. Voucher inexistente
        // --------------------------------------------------------------

        [Fact]
        public async Task ObterPorCodigo_QuandoVoucherNaoExiste_DeveRetornarNotFound()
        {
            // Arrange
            string codigo = "PROMO10";

            _voucherQueriesMock
                .Setup(x => x.ObterVoucherPorCodigo(codigo))
                .ReturnsAsync((VoucherDTO)null);

            // Act
            var result = await _controller.ObterPorCodigo(codigo);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        // --------------------------------------------------------------
        // 3. Voucher encontrado
        // --------------------------------------------------------------

        [Fact]
        public async Task ObterPorCodigo_QuandoVoucherExiste_DeveRetornarOk()
        {
            // Arrange
            string codigo = "PROMO10";

            var voucher = new VoucherDTO
            {
                Codigo = codigo,
                Percentual = 10
            };

            _voucherQueriesMock
                .Setup(x => x.ObterVoucherPorCodigo(codigo))
                .ReturnsAsync(voucher);

            // Act
            var result = await _controller.ObterPorCodigo(codigo);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
