using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NSE.Carrinho.API.Controllers;
using NSE.Carrinho.API.Data;
using NSE.Carrinho.API.Model;
using NSE.WebAPI.Core.Usuario;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Carrinho.API.Tests.Controllers
{
    public class CarrinhoControllerTests
    {
        private readonly Mock<IAspNetUser> _userMock;
        private readonly CarrinhoContext _context;
        private readonly CarrinhoController _controller;

        private readonly Guid _clienteId = Guid.NewGuid();

        public CarrinhoControllerTests()
        {
            _userMock = new Mock<IAspNetUser>();
            _userMock.Setup(u => u.ObterUserId()).Returns(_clienteId);

            var dbOptions = new DbContextOptionsBuilder<CarrinhoContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new CarrinhoContext(dbOptions);

            _controller = new CarrinhoController(_userMock.Object, _context);
        }

        private CarrinhoCliente CriarCarrinho()
        {
            var produtoId = Guid.NewGuid();

            var item = new CarrinhoItem
            {
                ProdutoId = produtoId,
                Nome = "Produto Teste",
                Quantidade = 2,
                Valor = 10m,
                Imagem = string.Empty
            };

            return new CarrinhoCliente(_clienteId)
            {
                Itens = new List<CarrinhoItem> { item }
            };
        }

        // ------------------------------------------------------------
        // TESTE: Obter carrinho retorna vazio quando não existe
        // ------------------------------------------------------------
        [Fact]
        public async Task ObterCarrinho_QuandoNaoExiste_DeveRetornarNovoCarrinho()
        {
            var result = await _controller.ObterCarrinho();

            Assert.NotNull(result);
            Assert.Empty(result.Itens);
        }

        // ------------------------------------------------------------
        // TESTE: Obter carrinho existente
        // ------------------------------------------------------------
        [Fact]
        public async Task ObterCarrinho_QuandoExiste_DeveRetornarCarrinho()
        {
            var carrinho = CriarCarrinho();

            _context.CarrinhoCliente.Add(carrinho);
            await _context.SaveChangesAsync();

            var result = await _controller.ObterCarrinho();

            Assert.NotNull(result);
            Assert.Single(result.Itens);
        }

        // ------------------------------------------------------------
        // TESTE: Adicionar item quando carrinho não existe
        // ------------------------------------------------------------
        [Fact]
        public async Task AdicionarItemCarrinho_QuandoCarrinhoNaoExiste_DeveCriarNovoCarrinho()
        {
            var produtoId = Guid.NewGuid();

            var item = new CarrinhoItem
            {
                ProdutoId = produtoId,
                Nome = "Novo Produto",
                Quantidade = 1,
                Valor = 20m,
                Imagem = string.Empty
            };

            var response = await _controller.AdicionarItemCarrinho(item);

            Assert.IsType<OkObjectResult>(response);
            Assert.Equal(1, _context.CarrinhoCliente.Count());
        }

        // ------------------------------------------------------------
        // TESTE: Adicionar item ao carrinho existente
        // ------------------------------------------------------------
        [Fact]
        public async Task AdicionarItemCarrinho_QuandoCarrinhoExiste_DeveAdicionarItem()
        {
            var carrinho = CriarCarrinho();

            _context.CarrinhoCliente.Add(carrinho);
            await _context.SaveChangesAsync();
                        
            _context.Entry(carrinho).State = EntityState.Detached;

            var item = new CarrinhoItem
            {
                ProdutoId = Guid.NewGuid(),
                Nome = "Item Novo",
                Quantidade = 1,
                Valor = 50m,
                Imagem = string.Empty
            };

            var result = await _controller.AdicionarItemCarrinho(item);

            Assert.IsType<OkObjectResult>(result);
            Assert.Equal(2, _context.CarrinhoItens.Count());
        }

        // ------------------------------------------------------------
        // TESTE: Atualizar item de carrinho
        // ------------------------------------------------------------
        [Fact]
        public async Task AtualizarItemCarrinho_DeveAtualizarQuantidade()
        {
            // Arrange
            var carrinho = CriarCarrinho();

            _context.CarrinhoCliente.Add(carrinho);
            await _context.SaveChangesAsync();
                       
            foreach (var entry in _context.ChangeTracker.Entries().ToList())
            {
                entry.State = EntityState.Detached;
            }

            var carrinhoDb = await _context.CarrinhoCliente
                .Include(x => x.Itens)
                .FirstAsync();

            var item = carrinhoDb.Itens.First();

            var atualizado = new CarrinhoItem
            {
                ProdutoId = item.ProdutoId,
                Nome = item.Nome,
                Quantidade = 5,
                Valor = item.Valor,
                Imagem = item.Imagem
            };

            // Act
            var response = await _controller.AtualizarItemCarrinho(item.ProdutoId, atualizado);

            var itemDb = _context.CarrinhoItens.First(i => i.ProdutoId == item.ProdutoId);

            Assert.Equal(5, itemDb.Quantidade);
            Assert.IsType<OkObjectResult>(response);
        }

        // ------------------------------------------------------------
        // TESTE: Remover item de carrinho
        // ------------------------------------------------------------
        [Fact]
        public async Task RemoverItemCarrinho_DeveRemoverComSucesso()
        {
            // =========================================
            // 1. ADICIONA O ITEM USANDO O ENDPOINT REAL
            // =========================================

            var produtoId = Guid.NewGuid();

            var item = new CarrinhoItem
            {
                ProdutoId = produtoId,
                Nome = "Produto Teste",
                Quantidade = 2,
                Valor = 10m,
                Imagem = "img"
            };

            var addResult = await _controller.AdicionarItemCarrinho(item);

            Assert.IsType<OkObjectResult>(addResult);

            // =========================================
            // 2. LIMPA TRACKING E RECUPERA DO BANCO
            // =========================================
            foreach (var entry in _context.ChangeTracker.Entries().ToList())
                entry.State = EntityState.Detached;

            var carrinhoDb = await _context.CarrinhoCliente
                .Include(c => c.Itens)
                .FirstAsync();

            var itemDb = carrinhoDb.Itens.First();

            // =========================================
            // 3. REMOVE VIA ENDPOINT REAL
            // =========================================
            var result = await _controller.RemoverItemCarrinho(itemDb.ProdutoId);

            // =========================================
            // 4. ASSERTS
            // =========================================

            Assert.IsType<OkObjectResult>(result);
            Assert.Empty(_context.CarrinhoItens);
        }  
    }
}
