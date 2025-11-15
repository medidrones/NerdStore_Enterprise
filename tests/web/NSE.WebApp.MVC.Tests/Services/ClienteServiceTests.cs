using FluentAssertions;
using Microsoft.Extensions.Options;
using NSE.WebApp.MVC.Extensions;
using NSE.WebApp.MVC.Services;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class ClienteServiceTests
{
    private readonly ClienteService _service;
    private readonly HttpMessageHandlerMock _handler;
    private readonly HttpClient _client;
    private readonly IOptions<AppSettings> _settings;

    public ClienteServiceTests()
    {
        _handler = new HttpMessageHandlerMock();

        _client = new HttpClient(_handler.HandlerMock.Object);
        
        _settings = Options.Create(new AppSettings
        {
            ClienteUrl = "http://localhost"
        });

        _service = new ClienteService(_client, _settings);
    }

    [Fact]
    public async Task ObterEndereco_DeveRetornarEndereco()
    {
        var json = "{\"logradouro\":\"Rua A\"}";

        _handler.SetupResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

        var result = await _service.ObterEndereco();

        result.Should().NotBeNull();
        result.Logradouro.Should().Be("Rua A");
    }
}
