using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NSE.Bff.Compras.Controllers;
using NSE.Bff.Compras.Models;
using NSE.Bff.Compras.Services;
using NSE.Bff.Compras.Services.gRPC;
using NSE.Core.Communication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Bff.Compras.Tests.Controllers
{
    public class CarrinhoControllerTests
    {
        private readonly Mock<ICarrinhoService> _carrinhoServiceMock;
        private readonly Mock<ICarrinhoGrpcService> _carrinhoGrpcMock;
        private readonly Mock<ICatalogoService> _catalogoServiceMock;
        private readonly Mock<IPedidoService> _pedidoServiceMock;

        private readonly CarrinhoController _controller;

        public CarrinhoControllerTests()
        {
            _carrinhoServiceMock = new Mock<ICarrinhoService>();
            _carrinhoGrpcMock = new Mock<ICarrinhoGrpcService>();
            _catalogoServiceMock = new Mock<ICatalogoService>();
            _pedidoServiceMock = new Mock<IPedidoService>();

            _controller = new CarrinhoController(
                _carrinhoServiceMock.Object,
                _carrinhoGrpcMock.Object,
                _catalogoServiceMock.Object,
                _pedidoServiceMock.Object
            );
        }

        private CarrinhoDTO CriarCarrinho()
        {
            return new CarrinhoDTO
            {
                Itens = new List<ItemCarrinhoDTO>
                {
                    new ItemCarrinhoDTO
                    {
                        ProdutoId = Guid.NewGuid(),
                        Nome = "Produto X",
                        Quantidade = 2,
                        Valor = 10
                    }
                }
            };
        }

        private ItemProdutoDTO CriarProdutoValido()
        {
            return new ItemProdutoDTO
            {
                Id = Guid.NewGuid(),
                Nome = "Produto Teste",
                Valor = 100,
                QuantidadeEstoque = 10,
                Imagem = "img.png"
            };
        }

        // ---------------------------------------------------------------
        // GET /compras/carrinho
        // ---------------------------------------------------------------
        [Fact]
        public async Task Index_DeveRetornarCustomResponseComCarrinho()
        {
            var carrinho = CriarCarrinho();

            _carrinhoGrpcMock
                .Setup(x => x.ObterCarrinho())
                .ReturnsAsync(carrinho);

            var result = await _controller.Index();

            result.Should().BeAssignableTo<ObjectResult>();
        }

        // ---------------------------------------------------------------
        // GET /compras/carrinho-quantidade
        // ---------------------------------------------------------------
        [Fact]
        public async Task ObterQuantidadeCarrinho_DeveRetornarSomatoria()
        {
            var carrinho = CriarCarrinho();

            _carrinhoGrpcMock
                .Setup(x => x.ObterCarrinho())
                .ReturnsAsync(carrinho);

            var result = await _controller.ObterQuantidadeCarrinho();

            result.Should().Be(2);
        }

        [Fact]
        public async Task ObterQuantidadeCarrinho_QuandoCarrinhoNull_DeveRetornarZero()
        {
            _carrinhoGrpcMock
                .Setup(x => x.ObterCarrinho())
                .ReturnsAsync((CarrinhoDTO)null);

            var result = await _controller.ObterQuantidadeCarrinho();

            result.Should().Be(0);
        }

        // ---------------------------------------------------------------
        // POST /compras/carrinho/items – adicionar item
        // ---------------------------------------------------------------
        [Fact]
        public async Task AdicionarItemCarrinho_QuandoValido_DeveRetornarCustomResponse()
        {
            var produto = CriarProdutoValido();
            var carrinho = CriarCarrinho();

            _catalogoServiceMock
                .Setup(x => x.ObterPorId(produto.Id))
                .ReturnsAsync(produto);

            _carrinhoServiceMock
                .Setup(x => x.ObterCarrinho())
                .ReturnsAsync(carrinho);

            _carrinhoServiceMock
                .Setup(x => x.AdicionarItemCarrinho(It.IsAny<ItemCarrinhoDTO>()))
                .ReturnsAsync(new ResponseResult());

            var item = new ItemCarrinhoDTO
            {
                ProdutoId = produto.Id,
                Quantidade = 1
            };

            var result = await _controller.AdicionarItemCarrinho(item);

            result.Should().BeOfType<OkObjectResult>(); // CORRETO
        }        

        // ---------------------------------------------------------------
        // DELETE /compras/carrinho/items/{produtoId}
        // ---------------------------------------------------------------    
        [Fact]
        public async Task RemoverItemCarrinho_QuandoValido_DeveRetornarCustomResponse()
        {
            var produto = CriarProdutoValido();
            var produtoId = produto.Id;

            _catalogoServiceMock
                .Setup(x => x.ObterPorId(produtoId))
                .ReturnsAsync(produto);

            _carrinhoServiceMock
                .Setup(x => x.RemoverItemCarrinho(produtoId))
                .ReturnsAsync(new ResponseResult());

            var result = await _controller.RemoverItemCarrinho(produtoId);

            result.Should().BeOfType<OkObjectResult>(); // CORRETO
        }

        // ---------------------------------------------------------------
        // POST /compras/carrinho/aplicar-voucher
        // ---------------------------------------------------------------        
        [Fact]
        public async Task AplicarVoucher_QuandoValido_DeveRetornarCustomResponse()
        {
            var voucher = new VoucherDTO { Codigo = "PROMO10" };
            var carrinho = CriarCarrinho();

            _pedidoServiceMock
                .Setup(x => x.ObterVoucherPorCodigo("PROMO10"))
                .ReturnsAsync(voucher);

            _carrinhoServiceMock
                .Setup(x => x.AplicarVoucherCarrinho(voucher))
                .ReturnsAsync(new ResponseResult());

            var result = await _controller.AplicarVoucher("PROMO10");

            result.Should().BeOfType<OkObjectResult>(); // CORRETO
        }
    }
}
