using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NSE.Bff.Compras.Controllers;
using NSE.Bff.Compras.Models;
using NSE.Bff.Compras.Services;
using NSE.Core.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

public class PedidoControllerTests
{
    private readonly Mock<ICatalogoService> _catalogoServiceMock;
    private readonly Mock<ICarrinhoService> _carrinhoServiceMock;
    private readonly Mock<IPedidoService> _pedidoServiceMock;
    private readonly Mock<IClienteService> _clienteServiceMock;

    private readonly PedidoController _controller;

    public PedidoControllerTests()
    {
        _catalogoServiceMock = new Mock<ICatalogoService>();
        _carrinhoServiceMock = new Mock<ICarrinhoService>();
        _pedidoServiceMock = new Mock<IPedidoService>();
        _clienteServiceMock = new Mock<IClienteService>();

        _controller = new PedidoController(
            _catalogoServiceMock.Object,
            _carrinhoServiceMock.Object,
            _pedidoServiceMock.Object,
            _clienteServiceMock.Object
        );
    }

    // --------------------------------------------------------------
    // 1. ADICIONAR PEDIDO
    // --------------------------------------------------------------
    [Fact]
    public async Task AdicionarPedido_QuandoValido_DeveRetornarOk()
    {
        var carrinho = CriarCarrinho();
        var produtos = carrinho.Itens.Select(i =>
            new ItemProdutoDTO
            {
                Id = i.ProdutoId,
                Nome = i.Nome,
                Valor = i.Valor
            });

        var endereco = new EnderecoDTO();

        _carrinhoServiceMock.Setup(x => x.ObterCarrinho()).ReturnsAsync(carrinho);
        _catalogoServiceMock.Setup(x => x.ObterItens(It.IsAny<IEnumerable<Guid>>())).ReturnsAsync(produtos);
        _clienteServiceMock.Setup(x => x.ObterEndereco()).ReturnsAsync(endereco);
        _pedidoServiceMock.Setup(x => x.FinalizarPedido(It.IsAny<PedidoDTO>()))
                          .ReturnsAsync(new ResponseResult());

        var pedido = new PedidoDTO();

        var result = await _controller.AdicionarPedido(pedido);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task AdicionarPedido_QuandoItemNaoExisteMaisNoCatalogo_DeveRetornarBadRequest()
    {
        var carrinho = CriarCarrinho();

        // PRODUTOS VAZIO → ITEM DO CARRINHO NÃO EXISTE MAIS NO CATÁLOGO
        var produtos = new List<ItemProdutoDTO>();

        _carrinhoServiceMock
            .Setup(x => x.ObterCarrinho())
            .ReturnsAsync(carrinho);

        _catalogoServiceMock
            .Setup(x => x.ObterItens(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(produtos);

        var result = await _controller.AdicionarPedido(new PedidoDTO());

        var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequest.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task AdicionarPedido_QuandoPrecoAlterou_DeveRetornarBadRequest()
    {
        var carrinho = CriarCarrinho();

        var produtos = carrinho.Itens.Select(i =>
            new ItemProdutoDTO
            {
                Id = i.ProdutoId,
                Nome = i.Nome,
                Valor = i.Valor + 10 // preço alterado
            });

        _carrinhoServiceMock
            .Setup(x => x.ObterCarrinho())
            .ReturnsAsync(carrinho);

        _catalogoServiceMock
            .Setup(x => x.ObterItens(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(produtos);

        // CORREÇÃO DA SINTAXE
        _carrinhoServiceMock
            .Setup(x => x.RemoverItemCarrinho(It.IsAny<Guid>()))
            .ReturnsAsync(new ResponseResult());

        _carrinhoServiceMock
            .Setup(x => x.AdicionarItemCarrinho(It.IsAny<ItemCarrinhoDTO>()))
            .ReturnsAsync(new ResponseResult());

        var result = await _controller.AdicionarPedido(new PedidoDTO());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // --------------------------------------------------------------
    // 2. ÚLTIMO PEDIDO
    // --------------------------------------------------------------
    [Fact]
    public async Task UltimoPedido_QuandoExiste_DeveRetornarOk()
    {
        var pedido = new PedidoDTO { Codigo = 10 };

        _pedidoServiceMock.Setup(x => x.ObterUltimoPedido())
            .ReturnsAsync(pedido);

        var result = await _controller.UltimoPedido();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UltimoPedido_QuandoNaoExiste_DeveRetornarBadRequest()
    {
        _pedidoServiceMock.Setup(x => x.ObterUltimoPedido())
            .ReturnsAsync((PedidoDTO)null);

        var result = await _controller.UltimoPedido();

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // --------------------------------------------------------------
    // 3. LISTA POR CLIENTE
    // --------------------------------------------------------------
    [Fact]
    public async Task ListaPorCliente_QuandoExiste_DeveRetornarOk()
    {
        var pedidos = new List<PedidoDTO>
        {
            new PedidoDTO { Codigo = 1 }
        };

        _pedidoServiceMock.Setup(x => x.ObterListaPorClienteId())
            .ReturnsAsync(pedidos);

        var result = await _controller.ListaPorCliente();

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListaPorCliente_QuandoNaoExiste_DeveRetornarNotFound()
    {
        _pedidoServiceMock.Setup(x => x.ObterListaPorClienteId())
            .ReturnsAsync((IEnumerable<PedidoDTO>)null);

        var result = await _controller.ListaPorCliente();

        result.Should().BeOfType<NotFoundResult>();
    }

    // --------------------------------------------------------------
    // AUXILIARES
    // --------------------------------------------------------------
    private CarrinhoDTO CriarCarrinho()
    {
        return new CarrinhoDTO
        {
            ValorTotal = 100,
            Desconto = 0,
            Itens = new List<ItemCarrinhoDTO>
            {
                new ItemCarrinhoDTO
                {
                    ProdutoId = Guid.NewGuid(),
                    Nome = "Produto X",
                    Quantidade = 1,
                    Valor = 100
                }
            }
        };
    }
}
