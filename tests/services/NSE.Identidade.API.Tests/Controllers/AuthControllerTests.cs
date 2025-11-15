using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NSE.Core.Messages.Integration;
using NSE.Identidade.API.Controllers;
using NSE.Identidade.API.Models;
using NSE.MessageBus;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace NSE.Identidade.API.Tests.Controllers
{
    public class AuthControllerTests : AuthControllerTestsBase
    {
        private readonly Mock<IMessageBus> _bus;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _bus = new Mock<IMessageBus>();
            _controller = new AuthController(AuthService, _bus.Object);
        }

        // ----------------------------
        // REGISTRO - SUCESSO
        // ----------------------------
        [Fact]
        public async Task Registrar_DeveRetornarSucesso_QuandoUsuarioCriadoComSucesso()
        {
            // Arrange
            var model = new UsuarioRegistro
            {
                Email = "teste@teste.com",
                Senha = "Senha123",
                Nome = "Teste",
                Cpf = "12345678900"
            };

            UserManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), model.Senha))
                .ReturnsAsync(IdentityResult.Success);

            UserManagerMock.Setup(x => x.FindByEmailAsync(model.Email))
                .ReturnsAsync(new IdentityUser { Email = model.Email, Id = Guid.NewGuid().ToString() });

            UserManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(new List<System.Security.Claims.Claim>());

            UserManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(new List<string>());

            _bus.Setup(x => x.RequestAsync<UsuarioRegistradoIntegrationEvent, ResponseMessage>(
                    It.IsAny<UsuarioRegistradoIntegrationEvent>()))
                .ReturnsAsync(new ResponseMessage(new FluentValidation.Results.ValidationResult()));

            // Act
            var result = await _controller.Registrar(model);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        // ----------------------------
        // REGISTRO - FALHA NO IDENTITY
        // ----------------------------
        [Fact]
        public async Task Registrar_DeveRetornarErros_QuandoIdentityFalha()
        {
            var model = new UsuarioRegistro
            {
                Email = "teste@teste.com",
                Senha = "Senha123"
            };

            var identityResult = IdentityResult.Failed(new IdentityError { Description = "Erro" });

            UserManagerMock.Setup(x => x.CreateAsync(It.IsAny<IdentityUser>(), model.Senha))
                .ReturnsAsync(identityResult);

            var result = await _controller.Registrar(model);
            var customResult = result as ObjectResult;

            Assert.NotNull(customResult);
            Assert.Equal(400, customResult.StatusCode);
        }   

        // ----------------------------
        // LOGIN - SUCESSO
        // ----------------------------
        [Fact]
        public async Task Login_DeveRetornarSucesso_QuandoCredenciaisValidas()
        {
            var model = new UsuarioLogin
            {
                Email = "teste@teste.com",
                Senha = "123"
            };

            // 1. Mock do PasswordSignInAsync
            SignInManagerMock.Setup(x => x.PasswordSignInAsync(
                    model.Email, model.Senha, false, true))
                .ReturnsAsync(SignInResult.Success);

            // 2. Mock para que o AuthenticationService consiga gerar o JWT
            UserManagerMock.Setup(x => x.FindByEmailAsync(model.Email))
                .ReturnsAsync(new IdentityUser
                {
                    Email = model.Email,
                    Id = Guid.NewGuid().ToString()
                });

            UserManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(new List<Claim>());

            UserManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }

        // ----------------------------
        // LOGIN - INVALIDO
        // ----------------------------
        [Fact]
        public async Task Login_DeveRetornarErro_QuandoCredenciaisInvalidas()
        {
            var model = new UsuarioLogin { Email = "x@x.com", Senha = "errado" };

            // Mock correto: SignInManager, não UserManager
            SignInManagerMock.Setup(x => x.PasswordSignInAsync(
                    model.Email, model.Senha, false, true))
                .ReturnsAsync(SignInResult.Failed);

            var result = await _controller.Login(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ----------------------------
        // LOGIN - LOCKOUT
        // ----------------------------
        [Fact]
        public async Task Login_DeveRetornarErro_QuandoUsuarioBloqueado()
        {
            var model = new UsuarioLogin { Email = "x@x.com", Senha = "errado" };

            SignInManagerMock.Setup(x => x.PasswordSignInAsync(
                    model.Email, model.Senha, false, true))
                .ReturnsAsync(SignInResult.LockedOut);

            var result = await _controller.Login(model);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ----------------------------
        // REFRESH TOKEN - INVALIDO
        // ----------------------------
        [Fact]
        public async Task RefreshToken_DeveRetornarErro_QuandoTokenInvalido()
        {
            var result = await _controller.RefreshToken("");

            Assert.IsType<BadRequestObjectResult>(result);
        }

        // ----------------------------
        // REFRESH TOKEN - SUCESSO
        // ----------------------------
        [Fact]
        public async Task RefreshToken_DeveRetornarNovoJwt_QuandoTokenValido()
        {
            var token = Guid.NewGuid();

            // Inserir refresh token válido no banco em memória
            DbContext.RefreshTokens.Add(new RefreshToken
            {
                Token = token,
                Username = "teste@teste.com",
                ExpirationDate = DateTime.UtcNow.AddHours(1)
            });
            await DbContext.SaveChangesAsync();

            // Mock das dependências do Jwt
            UserManagerMock.Setup(x => x.FindByEmailAsync("teste@teste.com"))
                .ReturnsAsync(new IdentityUser { Email = "teste@teste.com", Id = Guid.NewGuid().ToString() });

            UserManagerMock.Setup(x => x.GetClaimsAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(new List<Claim>());

            UserManagerMock.Setup(x => x.GetRolesAsync(It.IsAny<IdentityUser>()))
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _controller.RefreshToken(token.ToString());

            // Assert
            Assert.IsType<OkObjectResult>(result);
        }
    }
}
