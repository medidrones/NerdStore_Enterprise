using Moq;
using NSE.Catalogo.API.Controllers;
using NSE.Catalogo.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Catalogo.API.Tests.Controllers
{
    public class CatalogoControllerTests
    {
        private readonly Mock<IProdutoRepository> _produtoRepositoryMock;
        private readonly CatalogoController _controller;

        public CatalogoControllerTests()
        {
            _produtoRepositoryMock = new Mock<IProdutoRepository>();
            _controller = new CatalogoController(_produtoRepositoryMock.Object);
        }

        // ---------------------------------------------------
        // TESTE 1: Index()
        // ---------------------------------------------------
        [Fact(DisplayName = "Index - Deve retornar lista paginada com sucesso")]
        public async Task Index_DeveRetornarListaPaginada()
        {
            // Arrange
            var expected = new PagedResult<Produto>
            {
                List = new List<Produto>
                {
                    new Produto { Id = Guid.NewGuid(), Nome = "Produto A", Ativo = true }
                },
                PageIndex = 1,
                PageSize = 8,
                TotalResults = 1
            };

            _produtoRepositoryMock
                .Setup(r => r.ObterTodos(8, 1, null))
                .ReturnsAsync(expected);

            // Act
            var result = await _controller.Index();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expected.TotalResults, result.TotalResults);
            Assert.Single(result.List);
        }

        // ---------------------------------------------------
        // TESTE 2: ProdutoDetalhe()
        // ---------------------------------------------------
        [Fact(DisplayName = "ProdutoDetalhe - Deve retornar produto existente")]
        public async Task ProdutoDetalhe_DeveRetornarProduto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var produto = new Produto { Id = id, Nome = "Teste", Ativo = true };

            _produtoRepositoryMock
                .Setup(r => r.ObterPorId(id))
                .ReturnsAsync(produto);

            // Act
            var result = await _controller.ProdutoDetalhe(id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(id, result.Id);
        }

        [Fact(DisplayName = "ProdutoDetalhe - Deve retornar nulo quando produto não existir")]
        public async Task ProdutoDetalhe_DeveRetornarNulo_QuandoNaoExistir()
        {
            // Arrange
            var id = Guid.NewGuid();

            _produtoRepositoryMock
                .Setup(r => r.ObterPorId(id))
                .ReturnsAsync((Produto)null);

            // Act
            var result = await _controller.ProdutoDetalhe(id);

            // Assert
            Assert.Null(result);
        }

        // ---------------------------------------------------
        // TESTE 3: ObterProdutosPorId()
        // ---------------------------------------------------
        [Fact(DisplayName = "ObterProdutosPorId - Deve retornar coleção válida")]
        public async Task ObterProdutosPorId_DeveRetornarProdutos()
        {
            // Arrange
            var ids = "id1,id2,id3";

            var produtos = new List<Produto>
            {
                new Produto { Id = Guid.NewGuid(), Nome = "P1", Ativo = true },
                new Produto { Id = Guid.NewGuid(), Nome = "P2", Ativo = true }
            };

            _produtoRepositoryMock
                .Setup(r => r.ObterProdutosPorId(ids))
                .ReturnsAsync(produtos);

            // Act
            var result = await _controller.ObterProdutosPorId(ids);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, ((List<Produto>)result).Count);
        }

        [Fact(DisplayName = "ObterProdutosPorId - Deve retornar lista vazia quando nenhum produto encontrado")]
        public async Task ObterProdutosPorId_DeveRetornarListaVazia()
        {
            // Arrange
            var ids = "id1,id2";

            _produtoRepositoryMock
                .Setup(r => r.ObterProdutosPorId(ids))
                .ReturnsAsync(new List<Produto>());

            // Act
            var result = await _controller.ObterProdutosPorId(ids);

            // Assert
            Assert.Empty(result);
        }
    }
}
