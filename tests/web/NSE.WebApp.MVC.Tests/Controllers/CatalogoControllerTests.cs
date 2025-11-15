using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NSE.WebApp.MVC.Controllers;
using NSE.WebApp.MVC.Models;
using NSE.WebApp.MVC.Services;
using Xunit;
using System.Collections.Generic;

public class CatalogoControllerTests
{
    private readonly Mock<ICatalogoService> _catalogoServiceMock;
    private readonly CatalogoController _controller;

    public CatalogoControllerTests()
    {
        _catalogoServiceMock = new Mock<ICatalogoService>();

        _controller = new CatalogoController(_catalogoServiceMock.Object);
    }

    [Fact]
    public async Task Index_DeveRetornarViewComProdutos()
    {
        var produtos = new PagedViewModel<ProdutoViewModel>
        {
            List = new List<ProdutoViewModel>
            {
                new ProdutoViewModel { Id = Guid.NewGuid(), Nome = "Produto X" }
            },
            PageIndex = 1,
            PageSize = 8,
            Query = null,
            TotalResults = 1
        };

        _catalogoServiceMock
            .Setup(x => x.ObterTodos(8, 1, null))
            .ReturnsAsync(produtos);

        var result = await _controller.Index();

        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().Be(produtos);
    }

    [Fact]
    public async Task ProdutoDetalhe_DeveRetornarViewComProduto()
    {
        var produto = new ProdutoViewModel
        {
            Id = Guid.NewGuid(),
            Nome = "Produto Y"
        };

        _catalogoServiceMock
            .Setup(x => x.ObterPorId(produto.Id))
            .ReturnsAsync(produto);

        var result = await _controller.ProdutoDetalhe(produto.Id);

        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().Be(produto);
    }

    [Fact]
    public async Task ProdutoDetalhe_QuandoProdutoNaoEncontrado_DeveRetornarViewComNull()
    {
        _catalogoServiceMock
            .Setup(x => x.ObterPorId(It.IsAny<Guid>()))
            .ReturnsAsync((ProdutoViewModel)null);

        var result = await _controller.ProdutoDetalhe(Guid.NewGuid());

        result.Should().BeOfType<ViewResult>();
        ((ViewResult)result).Model.Should().BeNull();
    }
}
