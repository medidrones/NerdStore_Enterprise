using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Moq;
using NSE.WebAPI.Core.Usuario;
using NSE.WebApp.MVC.Extensions;
using NSE.WebApp.MVC.Models;
using NSE.WebApp.MVC.Services;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class AutenticacaoServiceTests
{
    private readonly AutenticacaoService _service;
    private readonly HttpMessageHandlerMock _handler;
    private readonly HttpClient _client;
    private readonly IOptions<AppSettings> _settings;
    private readonly Mock<IAspNetUser> _userMock;
    private readonly Mock<IAuthenticationService> _authMock;

    public AutenticacaoServiceTests()
    {
        _handler = new HttpMessageHandlerMock();
        _client = new HttpClient(_handler.HandlerMock.Object);

        _settings = Options.Create(new AppSettings
        {
            AutenticacaoUrl = "http://localhost"
        });

        _userMock = new Mock<IAspNetUser>();
        _authMock = new Mock<IAuthenticationService>();

        _service = new AutenticacaoService(
            _client,
            _settings,
            _userMock.Object,
            _authMock.Object);
    }

    [Fact]
    public async Task Login_DeveRetornarUsuarioRespostaLogin()
    {
        var json = "{\"accessToken\":\"abc\",\"expiresIn\":3600}";

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        _handler.SetupResponse(response);

        var result = await _service.Login(new UsuarioLogin
        {
            Email = "test@test.com",
            Senha = "123456"
        });

        result.Should().NotBeNull();
        result.AccessToken.Should().Be("abc");
    }
}
