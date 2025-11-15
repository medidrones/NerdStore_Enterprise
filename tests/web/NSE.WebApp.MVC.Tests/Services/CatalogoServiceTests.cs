using FluentAssertions;
using Microsoft.Extensions.Options;
using NSE.WebApp.MVC.Extensions;
using NSE.WebApp.MVC.Services;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class CatalogoServiceTests
{
    private readonly CatalogoService _service;
    private readonly HttpMessageHandlerMock _handler;
    private readonly HttpClient _client;
    private readonly IOptions<AppSettings> _settings;

    public CatalogoServiceTests()
    {
        _handler = new HttpMessageHandlerMock();

        _client = new HttpClient(_handler.HandlerMock.Object);

        _settings = Options.Create(new AppSettings
        {
            CatalogoUrl = "http://localhost"
        });

        _service = new CatalogoService(_client, _settings);
    }

    [Fact]
    public async Task ObterTodos_DeveRetornarPaginacao()
    {
        var json = "{\"list\":[{\"id\":\"11111111-1111-1111-1111-111111111111\",\"nome\":\"Produto X\"}]}";

        _handler.SetupResponse(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        var result = await _service.ObterTodos(8, 1, null);

        result.Should().NotBeNull();
        result.List.Should().HaveCount(1);
        result.List.First().Nome.Should().Be("Produto X");
    }
}
