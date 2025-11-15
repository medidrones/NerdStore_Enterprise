using Microsoft.Extensions.DependencyInjection;
using Moq;
using NSE.Catalogo.API.Models;
using NSE.Catalogo.API.Services;
using NSE.Core.DomainObjects;
using NSE.Core.Messages.Integration;
using NSE.MessageBus;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Catalogo.API.Tests.Services
{
    public class CatalogoIntegrationHandlerTests
    {
        private readonly Mock<IMessageBus> _busMock;
        private readonly Mock<IServiceProvider> _serviceProviderMock;
        private readonly Mock<IServiceScope> _serviceScopeMock;
        private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private readonly Mock<IProdutoRepository> _produtoRepositoryMock;

        public CatalogoIntegrationHandlerTests()
        {
            _busMock = new Mock<IMessageBus>();
            _serviceProviderMock = new Mock<IServiceProvider>();
            _serviceScopeMock = new Mock<IServiceScope>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _produtoRepositoryMock = new Mock<IProdutoRepository>();

            // ServiceScope
            _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(_serviceProviderMock.Object);
            _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);
            _serviceProviderMock.Setup(p => p.GetService(typeof(IServiceScopeFactory)))
                                .Returns(_serviceScopeFactoryMock.Object);

            // Repository
            _serviceProviderMock.Setup(p => p.GetService(typeof(IProdutoRepository)))
                                .Returns(_produtoRepositoryMock.Object);
        }

        private CatalogoIntegrationHandler CreateHandler()
        {
            return new CatalogoIntegrationHandler(_serviceProviderMock.Object, _busMock.Object);
        }

        // ---------------------------------------------------------
        // Cenário 1: Produtos insuficientes -> Cancelar pedido
        // ---------------------------------------------------------
        [Fact(DisplayName = "BaixarEstoque - Deve cancelar pedido quando produtos não encontrados")]
        public async Task BaixarEstoque_DeveCancelarPedido_QuandoProdutosIncompletos()
        {
            // Arrange
            var message = new PedidoAutorizadoIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(),
                new Dictionary<Guid, int>
                {
            { Guid.NewGuid(), 1 },
            { Guid.NewGuid(), 2 }
                });

            _produtoRepositoryMock.Setup(r => r.ObterProdutosPorId(It.IsAny<string>()))
                                  .ReturnsAsync(new List<Produto>());

            var handler = CreateHandler();

            // Act
            var method = handler.GetType()
                .GetMethod("BaixarEstoque", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var task = (Task)method.Invoke(handler, new object[] { message });
            await task;

            // Assert
            _busMock.Verify(b => b.PublishAsync(It.IsAny<PedidoCanceladoIntegrationEvent>()), Times.Once);
        }

        // ---------------------------------------------------------
        // Cenário 2: Estoque insuficiente -> Cancelar pedido
        // ---------------------------------------------------------
        [Fact(DisplayName = "BaixarEstoque - Deve cancelar quando algum item não tiver estoque")]
        public async Task BaixarEstoque_DeveCancelarPedido_QuandoEstoqueInsuficiente()
        {
            // Arrange
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var message = new PedidoAutorizadoIntegrationEvent(Guid.NewGuid(), Guid.NewGuid(),
                new Dictionary<Guid, int>
                {
                    { id1, 5 },
                    { id2, 5 }
                });

            // Produtos reais com estoque insuficiente
            var produto1 = new Produto();
            var produto2 = new Produto();            

            _produtoRepositoryMock
                .Setup(r => r.ObterProdutosPorId(It.IsAny<string>()))
                .ReturnsAsync(new List<Produto> { produto1, produto2 });

            var handler = CreateHandler();

            // Act
            var method = handler.GetType()
                .GetMethod("BaixarEstoque", BindingFlags.NonPublic | BindingFlags.Instance);

            var task = (Task)method.Invoke(handler, new object[] { message });
            await task;

            // Assert
            _busMock.Verify(b => b.PublishAsync(It.IsAny<PedidoCanceladoIntegrationEvent>()), Times.Once);
            _busMock.Verify(b => b.PublishAsync(It.IsAny<PedidoBaixadoEstoqueIntegrationEvent>()), Times.Never);
        }

        // ---------------------------------------------------------
        // Cenário 3: Estoque suficiente -> Baixar estoque e publicar evento
        // ---------------------------------------------------------
        [Fact(DisplayName = "BaixarEstoque - Deve baixar estoque e publicar evento de sucesso")]
        public async Task BaixarEstoque_DeveBaixarEstoque_QuandoTudoOk()
        {
            // Arrange
            var id = Guid.NewGuid();

            var message = new PedidoAutorizadoIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Dictionary<Guid, int> { { id, 3 } });

            // Produto REAL compatível com o domínio
            var produto = new Produto
            {
                Id = id,
                Nome = "Produto Teste",
                Descricao = "Desc",
                Valor = 100,
                Ativo = true,
                QuantidadeEstoque = 10,
                DataCadastro = DateTime.Now,
                Imagem = "img.jpg"
            };

            _produtoRepositoryMock
                .Setup(r => r.ObterProdutosPorId(It.IsAny<string>()))
                .ReturnsAsync(new List<Produto> { produto });

            _produtoRepositoryMock
                .Setup(r => r.UnitOfWork.Commit())
                .ReturnsAsync(true);

            var handler = CreateHandler();

            // Executa BaixarEstoque via reflection
            var method = handler.GetType()
                .GetMethod("BaixarEstoque", BindingFlags.NonPublic | BindingFlags.Instance);

            var task = (Task)method.Invoke(handler, new object[] { message });
            await task;

            // Assert
            _produtoRepositoryMock.Verify(
                r => r.Atualizar(It.Is<Produto>(p => p.Id == id)),
                Times.Once);

            _busMock.Verify(
                b => b.PublishAsync(It.IsAny<PedidoBaixadoEstoqueIntegrationEvent>()),
                Times.Once);

            _busMock.Verify(
                b => b.PublishAsync(It.IsAny<PedidoCanceladoIntegrationEvent>()),
                Times.Never);
        }

        // ---------------------------------------------------------
        // Cenário 4: Commit falhou -> DomainException
        // ---------------------------------------------------------
        [Fact(DisplayName = "BaixarEstoque - Deve lançar DomainException quando commit falhar")]
        public async Task BaixarEstoque_DeveLancarDomainException_QuandoCommitFalhar()
        {
            // Arrange
            var id = Guid.NewGuid();

            var message = new PedidoAutorizadoIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new Dictionary<Guid, int> { { id, 2 } }
            );

            // Produto real (entidade não mockada)
            var produto = new Produto
            {
                Id = id,
                Nome = "Produto Teste",
                Descricao = "Desc",
                Valor = 10,
                Ativo = true,
                QuantidadeEstoque = 10,
                DataCadastro = DateTime.Now,
                Imagem = "imagem.jpg"
            };

            _produtoRepositoryMock
                .Setup(r => r.ObterProdutosPorId(It.IsAny<string>()))
                .ReturnsAsync(new List<Produto> { produto });

            _produtoRepositoryMock
                .Setup(r => r.UnitOfWork.Commit())
                .ReturnsAsync(false);

            var handler = CreateHandler();

            // Reflection do método privado
            var method = handler.GetType()
                .GetMethod("BaixarEstoque", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            async Task Act()
            {
                try
                {
                    var task = (Task)method.Invoke(handler, new object[] { message });
                    await task;
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
            }

            // Assert
            await Assert.ThrowsAsync<DomainException>(Act);
        }
    }
}
