using FluentAssertions;
using Microsoft.Extensions.Options;
using NSE.WebApp.MVC.Extensions;
using NSE.WebApp.MVC.Services;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class ComprasBffServiceTests
{
    private readonly ComprasBffService _service;
    private readonly HttpMessageHandlerMock _handler;
    private readonly HttpClient _client;
    private readonly IOptions<AppSettings> _settings;

    public ComprasBffServiceTests()
    {
        _handler = new HttpMessageHandlerMock();

        _client = new HttpClient(_handler.HandlerMock.Object);

        _settings = Options.Create(new AppSettings
        {
            ComprasBffUrl = "http://localhost"
        });

        _service = new ComprasBffService(_client, _settings);
    }

    [Fact]
    public async Task AplicarVoucherCarrinho_DeveRetornarResponseResult()
    {
        var json = "{\"errors\":{}}";

        _handler.SetupResponse(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var result = await _service.AplicarVoucherCarrinho("PROMO10");

        result.Should().NotBeNull();
    }
}
