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

public class IdentidadeControllerTests
{
    private readonly Mock<IAutenticacaoService> _authServiceMock;
    private readonly IdentidadeController _controller;

    public IdentidadeControllerTests()
    {
        _authServiceMock = new Mock<IAutenticacaoService>();
        _controller = new IdentidadeController(_authServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        var tempDataProvider = new Mock<ITempDataProvider>();
        var tempData = new TempDataDictionary(httpContext, tempDataProvider.Object);

        _controller.TempData = tempData;
    }

    [Fact]
    public async Task Login_DeveRetornarRedirect_QuandoSucesso()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.Login(It.IsAny<UsuarioLogin>()))
            .ReturnsAsync(new UsuarioRespostaLogin());

        // Act
        var result = await _controller.Login(new UsuarioLogin());

        // Assert
        result.Should().BeOfType<RedirectToActionResult>();
    }

    [Fact]
    public async Task Login_DeveRetornarView_QuandoFalha()
    {
        // Arrange
        _authServiceMock
            .Setup(x => x.Login(It.IsAny<UsuarioLogin>()))
            .ReturnsAsync(new UsuarioRespostaLogin
            {
                ResponseResult = new ResponseResult
                {
                    Errors = new ResponseErrorMessages
                    {
                        Mensagens = { "Credenciais inválidas" }
                    }
                }
            });

        // Act
        var result = await _controller.Login(new UsuarioLogin());

        // Assert
        result.Should().BeOfType<ViewResult>();
    }

    [Fact]
    public async Task Registrar_DeveRetornarViewComErro_QuandoFalha()
    {
        _authServiceMock
            .Setup(x => x.Registro(It.IsAny<UsuarioRegistro>()))
            .ReturnsAsync(new UsuarioRespostaLogin
            {
                ResponseResult = new ResponseResult
                {
                    Errors = new ResponseErrorMessages
                    {
                        Mensagens = { "Erro de validação" }
                    }
                }
            });

        var result = await _controller.Registro(new UsuarioRegistro());

        result.Should().BeOfType<ViewResult>();
    }
}
