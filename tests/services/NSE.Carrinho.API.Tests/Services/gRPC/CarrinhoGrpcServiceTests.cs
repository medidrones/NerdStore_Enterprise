using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NSE.Carrinho.API.Data;
using NSE.Carrinho.API.Model;
using NSE.Carrinho.API.Services.gRPC;
using NSE.WebAPI.Core.Usuario;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Carrinho.API.Tests.Services.gRPC
{
    public class CarrinhoGrpcServiceTests
    {
        private readonly Mock<ILogger<CarrinhoGrpcService>> _loggerMock = new Mock<ILogger<CarrinhoGrpcService>>();
        private readonly Mock<IAspNetUser> _userMock = new Mock<IAspNetUser>();

        private CarrinhoContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<CarrinhoContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new CarrinhoContext(options);
        }

        private ServerCallContext CreateFakeContext()
        {
            return new FakeServerCallContext();
        }

        // -----------------------
        // Helper resiliente para criar CarrinhoItem
        // -----------------------
        private object CreateCarrinhoItemInstance(Guid produtoId, string nome, int quantidade, decimal valor, string imagem)
        {
            var itemType = typeof(CarrinhoItem);

            var ctors = itemType.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var ctor in ctors)
            {
                var parameters = ctor.GetParameters();

                if (parameters.Length >= 4 &&
                    parameters.Any(p => p.ParameterType == typeof(Guid)) &&
                    parameters.Any(p => p.ParameterType == typeof(string)) &&
                    parameters.Any(p => p.ParameterType == typeof(int) || p.ParameterType == typeof(short)) &&
                    parameters.Any(p => p.ParameterType == typeof(decimal) || p.ParameterType == typeof(double) || p.ParameterType == typeof(float)))
                {
                    var args = parameters.Select(p =>
                    {
                        if (p.ParameterType == typeof(Guid)) return (object)produtoId;
                        if (p.ParameterType == typeof(string)) return (object)nome;
                        if (p.ParameterType == typeof(int) || p.ParameterType == typeof(short)) return (object)quantidade;
                        if (p.ParameterType == typeof(decimal)) return (object)valor;
                        if (p.ParameterType == typeof(double)) return (object)(double)valor;
                        if (p.ParameterType == typeof(float)) return (object)(float)valor;

                        return p.HasDefaultValue ? p.DefaultValue : null;
                    }).ToArray();

                    try
                    {
                        return ctor.Invoke(args);
                    }
                    catch
                    {
                        // ignorar e tentar outro construtor
                    }
                }
            }

            var parameterlessCtor = ctors.FirstOrDefault(c => c.GetParameters().Length == 0);

            if (parameterlessCtor != null)
            {
                var instance = parameterlessCtor.Invoke(new object[0]);

                void TrySet(string name, object value)
                {
                    var prop = itemType.GetProperty(name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

                    if (prop != null && prop.CanWrite)
                    {
                        try { prop.SetValue(instance, value); return; } catch { }
                    }

                    var field = itemType.GetField("<" + name + ">k__BackingField",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                    if (field != null)
                    {
                        try { field.SetValue(instance, value); return; } catch { }
                    }

                    var directField = itemType.GetField(name,
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                    if (directField != null)
                    {
                        try { directField.SetValue(instance, value); return; } catch { }
                    }
                }

                TrySet("ProdutoId", produtoId);
                TrySet("Nome", nome);
                TrySet("Quantidade", quantidade);
                TrySet("Valor", valor);
                TrySet("Imagem", imagem);
                TrySet("Id", Guid.NewGuid());

                return instance;
            }

            var fallback = Activator.CreateInstance(itemType, true);

            if (fallback != null)
                return fallback;

            throw new InvalidOperationException("Não foi possível instanciar CarrinhoItem no teste.");
        }

        // -----------------------------------------
        // Testes
        // -----------------------------------------
        [Fact]
        public async Task ObterCarrinho_DeveRetornarCarrinhoExistente()
        {
            var userId = Guid.NewGuid();
            _userMock.Setup(u => u.ObterUserId()).Returns(userId);

            var context = CreateInMemoryContext("carrinho_existente_v2");

            var carrinho = new CarrinhoCliente(userId);

            var itemObj = CreateCarrinhoItemInstance(Guid.NewGuid(), "Produto Teste", 2, 50m, "img.png");

            var carrinhoType = typeof(CarrinhoCliente);

            var itensProp = carrinhoType.GetProperty("Itens",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (itensProp != null)
            {
                var itensCollection = itensProp.GetValue(carrinho);
                var addMethod = itensCollection.GetType().GetMethod("Add");
                addMethod.Invoke(itensCollection, new object[] { itemObj });
            }

            context.CarrinhoCliente.Add(carrinho);
            await context.SaveChangesAsync();

            var service = new CarrinhoGrpcService(_loggerMock.Object, _userMock.Object, context);

            var response = await service.ObterCarrinho(new ObterCarrinhoRequest(), CreateFakeContext());

            Assert.Equal(carrinho.Id.ToString(), response.Id);
            Assert.Single(response.Itens);
            Assert.Contains("Produto Teste", response.Itens[0].Nome);
        }

        [Fact]
        public async Task ObterCarrinho_QuandoNaoExistir_DeveRetornarVazio()
        {
            var userId = Guid.NewGuid();
            _userMock.Setup(u => u.ObterUserId()).Returns(userId);

            var context = CreateInMemoryContext("carrinho_vazio_v2");

            var service = new CarrinhoGrpcService(_loggerMock.Object, _userMock.Object, context);

            var response = await service.ObterCarrinho(new ObterCarrinhoRequest(), CreateFakeContext());

            Assert.NotNull(response);
            Assert.Empty(response.Itens);
            Assert.False(response.Voucherutilizado);
        }

        [Fact]
        public async Task ObterCarrinho_ComVoucher_DeveMapearCorretamente()
        {
            var userId = Guid.NewGuid();
            _userMock.Setup(u => u.ObterUserId()).Returns(userId);

            var context = CreateInMemoryContext("carrinho_voucher_v2");

            var carrinho = new CarrinhoCliente(userId);
            carrinho.VoucherUtilizado = true;
            carrinho.Desconto = 10;

            carrinho.Voucher = new Voucher
            {
                Codigo = "PROMO10",
                Percentual = 10,
                TipoDesconto = TipoDescontoVoucher.Porcentagem
            };

            context.CarrinhoCliente.Add(carrinho);
            await context.SaveChangesAsync();

            var service = new CarrinhoGrpcService(_loggerMock.Object, _userMock.Object, context);

            var response = await service.ObterCarrinho(new ObterCarrinhoRequest(), CreateFakeContext());

            Assert.NotNull(response.Voucher);
            Assert.Equal("PROMO10", response.Voucher.Codigo);
            Assert.Equal(10, response.Voucher.Percentual);
        }
    }
}
