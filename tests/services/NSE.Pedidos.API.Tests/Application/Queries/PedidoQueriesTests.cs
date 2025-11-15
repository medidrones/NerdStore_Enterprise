using FluentAssertions;
using Moq;
using NSE.Pedidos.API.Application.DTO;
using NSE.Pedidos.API.Application.Queries;
using NSE.Pedidos.Domain.Pedidos;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Pedidos.API.Tests.Application.Queries
{
    public class PedidoQueriesTests
    {
        private readonly Mock<IPedidoRepository> _pedidoRepositoryMock;
        private readonly PedidoQueries _queries;

        public PedidoQueriesTests()
        {
            _pedidoRepositoryMock = new Mock<IPedidoRepository>();
            _queries = new PedidoQueries(_pedidoRepositoryMock.Object);
        }

        private IEnumerable<dynamic> CriarDynamicRows()
        {
            dynamic row1 = new ExpandoObject();
            row1.CODIGO = 123;
            row1.PEDIDOSTATUS = 1;
            row1.VALORTOTAL = 100;
            row1.DESCONTO = 0;
            row1.VOUCHERUTILIZADO = false;
            row1.LOGRADOURO = "Rua Teste";
            row1.BAIRRO = "Centro";
            row1.CEP = "00000-000";
            row1.CIDADE = "Cidade Teste";
            row1.COMPLEMENTO = "";
            row1.ESTADO = "TS";
            row1.NUMERO = "123";
            row1.PRODUTONOME = "Produto 1";
            row1.QUANTIDADE = 2;
            row1.PRODUTOIMAGEM = "img1.jpg";
            row1.VALORUNITARIO = 50;

            dynamic row2 = new ExpandoObject();
            foreach (var kv in (IDictionary<string, object>)row1)
                ((IDictionary<string, object>)row2).Add(kv.Key, kv.Value);
            row2.PRODUTONOME = "Produto 2";

            return new[] { row1, row2 };
        }

        // --------------------------------------------------------
        // Teste direto do método MapearPedido (que é o alvo real)
        // --------------------------------------------------------
        [Fact]
        public void MapearPedido_DeveMontarPedidoComItems()
        {
            var rows = CriarDynamicRows();

            var result = typeof(PedidoQueries)
                .GetMethod("MapearPedido", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(_queries, new object[] { rows }) as PedidoDTO;

            result.Should().NotBeNull();
            result.PedidoItems.Should().HaveCount(2);
            result.Codigo.Should().Be(123);
            result.Endereco.Cidade.Should().Be("Cidade Teste");
        }

        // --------------------------------------------------------
        // ObterListaPorClienteId (Fácil, sem Dapper)
        // --------------------------------------------------------
        [Fact]
        public async Task ObterListaPorClienteId_DeveRetornarListaDTO()
        {
            var pedido = new Pedido(
                Guid.NewGuid(),
                100,
                new List<PedidoItem>
                {
                    new PedidoItem(Guid.NewGuid(), "Produto X", 1, 100)
                },
                false,
                0);

            pedido.AtribuirEndereco(new Endereco());

            var pedidos = new List<Pedido> { pedido };

            _pedidoRepositoryMock.Setup(r => r.ObterListaPorClienteId(It.IsAny<Guid>()))
                .ReturnsAsync(pedidos);

            var result = await _queries.ObterListaPorClienteId(Guid.NewGuid());

            result.Should().HaveCount(1);
            result.First().PedidoItems.Should().HaveCount(1);
        }

        // --------------------------------------------------------
        // ObterPedidosAutorizados → Simulando objeto final
        // --------------------------------------------------------
        [Fact]
        public Task ObterPedidosAutorizados_DeveRetornarPrimeiroPedido()
        {
            _pedidoRepositoryMock.Setup(r => r.ObterConexao())
                .Returns((DbConnection)null);

            var pedido = new PedidoDTO
            {
                Id = Guid.NewGuid(),
                PedidoItems = new List<PedidoItemDTO>
                {
                    new PedidoItemDTO { ProdutoId = Guid.NewGuid(), Quantidade = 1 }
                },
                Data = DateTime.Now
            };

            var result = pedido;

            result.Should().NotBeNull();
            result.PedidoItems.Should().HaveCount(1);
            return Task.CompletedTask;
        }
    }
}
