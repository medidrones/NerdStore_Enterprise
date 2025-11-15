using FluentAssertions;
using Moq;
using NSE.Pedidos.API.Application.Queries;
using NSE.Pedidos.Domain;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Pedidos.API.Tests.Application.Queries
{
    public class VoucherQueriesTests
    {
        private readonly Mock<IVoucherRepository> _voucherRepositoryMock;
        private readonly VoucherQueries _queries;

        public VoucherQueriesTests()
        {
            _voucherRepositoryMock = new Mock<IVoucherRepository>();
            _queries = new VoucherQueries(_voucherRepositoryMock.Object);
        }

        // ---------------------------------------------------------
        // 1. Voucher não encontrado → retorna null
        // ---------------------------------------------------------
        [Fact]
        public async Task ObterVoucherPorCodigo_QuandoNaoExiste_DeveRetornarNull()
        {
            _voucherRepositoryMock
                .Setup(r => r.ObterVoucherPorCodigo("PROMO10"))
                .ReturnsAsync((Voucher)null);

            var result = await _queries.ObterVoucherPorCodigo("PROMO10");

            result.Should().BeNull();
        }

        // ---------------------------------------------------------
        // 2. Voucher encontrado porém inválido → retorna null
        // ---------------------------------------------------------
        [Fact]
        public async Task ObterVoucherPorCodigo_QuandoInvalido_DeveRetornarNull()
        {
            // Voucher inválido simplesmente porque está expirado
            var voucher = new Voucher();

            _voucherRepositoryMock
                .Setup(r => r.ObterVoucherPorCodigo("PROMO10"))
                .ReturnsAsync(voucher);

            var result = await _queries.ObterVoucherPorCodigo("PROMO10");

            result.Should().BeNull();
        }        
    }
}
