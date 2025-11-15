using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NSE.Core.Communication;
using NSE.WebApp.MVC.Controllers;
using NSE.WebApp.MVC.Models;
using NSE.WebApp.MVC.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

public class CarrinhoControllerTests
{
    private readonly Mock<IComprasBffService> _comprasBffServiceMock;
    private readonly CarrinhoController _controller;

    public CarrinhoControllerTests()
    {
        _comprasBffServiceMock = new Mock<IComprasBffService>();

        _controller = new CarrinhoController(
            _comprasBffServiceMock.Object
        );
    }

    [Fact]
    public async Task Index_DeveRetornarViewComCarrinho()
    {
        // Arrange
        var carrinho = new CarrinhoViewModel
        {
            ValorTotal = 250,
            Itens = new List<ItemCarrinhoViewModel>()
        };

        _comprasBffServiceMock
            .Setup(c => c.ObterCarrinho())
            .ReturnsAsync(carrinho);

        // Act
        var result = await _controller.Index();

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.Model.Should().Be(carrinho);
    }

    [Fact]
    public async Task AdicionarItemCarrinho_QuandoSucesso_DeveRedirecionarParaIndex()
    {
        // Arrange
        var item = new ItemCarrinhoViewModel
        {
            ProdutoId = Guid.NewGuid(),
            Quantidade = 1
        };

        _comprasBffServiceMock
            .Setup(c => c.AdicionarItemCarrinho(item))
            .ReturnsAsync(new ResponseResult());

        // Act
        var result = await _controller.AdicionarItemCarrinho(item);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Index");
    }

    [Fact]
    public async Task AdicionarItemCarrinho_QuandoErro_DeveRetornarViewIndex()
    {
        // Arrange
        var item = new ItemCarrinhoViewModel
        {
            ProdutoId = Guid.NewGuid(),
            Quantidade = 1
        };

        var erro = new ResponseResult();
        erro.Errors = new ResponseErrorMessages();
        erro.Errors.Mensagens.Add("Erro de teste");

        _comprasBffServiceMock
            .Setup(c => c.AdicionarItemCarrinho(item))
            .ReturnsAsync(erro);

        _comprasBffServiceMock
            .Setup(c => c.ObterCarrinho())
            .ReturnsAsync(new CarrinhoViewModel());

        // Act
        var result = await _controller.AdicionarItemCarrinho(item);

        // Assert
        result.Should().BeOfType<ViewResult>()
            .Which.ViewName.Should().Be("Index");
    }

    [Fact]
    public async Task RemoverItemCarrinho_QuandoSucesso_DeveRedirecionarIndex()
    {
        var produtoId = Guid.NewGuid();

        _comprasBffServiceMock
            .Setup(c => c.RemoverItemCarrinho(produtoId))
            .ReturnsAsync(new ResponseResult());

        var result = await _controller.RemoverItemCarrinho(produtoId);

        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task AtualizarItemCarrinho_QuandoSucesso_DeveRedirecionarIndex()
    {
        var produtoId = Guid.NewGuid();
        var quantidade = 3;

        _comprasBffServiceMock
            .Setup(c => c.AtualizarItemCarrinho(
                produtoId,
                It.Is<ItemCarrinhoViewModel>(i => i.Quantidade == quantidade)))
            .ReturnsAsync(new ResponseResult());

        var result = await _controller.AtualizarItemCarrinho(produtoId, quantidade);

        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task AplicarVoucher_QuandoSucesso_DeveRedirecionarIndex()
    {
        var voucher = "PROMO10";

        _comprasBffServiceMock
            .Setup(c => c.AplicarVoucherCarrinho(voucher))
            .ReturnsAsync(new ResponseResult());

        var result = await _controller.AplicarVoucher(voucher);

        result.Should().BeOfType<RedirectToActionResult>();
    }
}
