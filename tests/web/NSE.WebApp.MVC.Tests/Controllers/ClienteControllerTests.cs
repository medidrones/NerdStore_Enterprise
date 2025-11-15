using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NSE.Core.Communication;
using NSE.WebApp.MVC.Controllers;
using NSE.WebApp.MVC.Models;
using NSE.WebApp.MVC.Services;
using System.Threading.Tasks;
using Xunit;

public class ClienteControllerTests
{
    private readonly Mock<IClienteService> _clienteServiceMock;
    private readonly ClienteController _controller;

    public ClienteControllerTests()
    {
        _clienteServiceMock = new Mock<IClienteService>();
        _controller = new ClienteController(_clienteServiceMock.Object);
                
        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

        _controller.TempData = tempData;
    }

    [Fact]
    public async Task NovoEndereco_QuandoSucesso_DeveRedirecionarParaEnderecoEntrega()
    {
        // Arrange
        var endereco = new EnderecoViewModel();

        _clienteServiceMock
            .Setup(x => x.AdicionarEndereco(endereco))
            .ReturnsAsync(new ResponseResult());

        // Act
        var result = await _controller.NovoEndereco(endereco);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();

        var redirect = (RedirectToActionResult)result;
        redirect.ActionName.Should().Be("EnderecoEntrega");
        redirect.ControllerName.Should().Be("Pedido");
    }

    [Fact]
    public async Task NovoEndereco_QuandoErroDeValidacao_DeveRetornarRedirectComErrosEmTempData()
    {
        // Arrange
        var endereco = new EnderecoViewModel();

        var response = new ResponseResult();
        response.Errors = new ResponseErrorMessages();
        response.Errors.Mensagens.Add("Erro de teste");

        _clienteServiceMock
            .Setup(x => x.AdicionarEndereco(endereco))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.NovoEndereco(endereco);

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();

        _controller.TempData.ContainsKey("Erros").Should().BeTrue();
    }
}
