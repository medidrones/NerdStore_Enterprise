using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using NSE.Identidade.API.Data;
using NSE.Identidade.API.Extensions;
using NSE.Identidade.API.Models;
using NSE.Identidade.API.Services;
using NSE.WebAPI.Core.Identidade;
using NSE.WebAPI.Core.Usuario;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace NSE.Identidade.API.Tests.Services
{
    public class AuthenticationServiceTests
    {
        private readonly AuthenticationService _service;
        private readonly ApplicationDbContext _context;
        private readonly Mock<UserManager<IdentityUser>> _userManager;
        private readonly Mock<SignInManager<IdentityUser>> _signInManager;
        private readonly Mock<IJsonWebKeySetService> _jwks;
        private readonly Mock<IAspNetUser> _aspNetUser;

        public AuthenticationServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase("teste_db")
                .Options;

            _context = new ApplicationDbContext(options);

            _userManager = MockUserManager();
            _signInManager = MockSignInManager();
            _jwks = new Mock<IJsonWebKeySetService>();
            _aspNetUser = new Mock<IAspNetUser>();

            _aspNetUser.Setup(x => x.ObterHttpContext())
                .Returns(new DefaultHttpContext());         

            var appSettings = Options.Create(new AppSettings());
            var tokenSettings = Options.Create(new AppTokenSettings { RefreshTokenExpiration = 2 });

            _service = new AuthenticationService(
                _signInManager.Object,
                _userManager.Object,
                appSettings,
                tokenSettings,
                _context,
                _jwks.Object,
                _aspNetUser.Object
            );
        }

        // ----------------------------
        // GERAR JWT
        // ----------------------------
        [Fact]
        public async Task GerarJwt_DeveGerarAccessTokenERefreshToken()
        {
            var user = new IdentityUser
            {
                Email = "teste@teste.com",
                Id = Guid.NewGuid().ToString()
            };

            _userManager.Setup(x => x.FindByEmailAsync(user.Email))
                .ReturnsAsync(user);

            _userManager.Setup(x => x.GetClaimsAsync(user))
                .ReturnsAsync(new List<Claim>());

            _userManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            var result = await _service.GerarJwt(user.Email);

            Assert.NotNull(result.AccessToken);
            Assert.NotNull(result.RefreshToken);
        }

        // ----------------------------
        // OBTER REFRESH TOKEN
        // ----------------------------
        [Fact]
        public async Task ObterRefreshToken_DeveRetornarNull_QuandoExpirado()
        {
            var token = new RefreshToken
            {
                Username = "x",
                Token = Guid.NewGuid(),
                ExpirationDate = DateTime.UtcNow.AddHours(-1)
            };

            _context.RefreshTokens.Add(token);
            await _context.SaveChangesAsync();

            var result = await _service.ObterRefreshToken(token.Token);

            Assert.Null(result);
        }

        private Mock<UserManager<IdentityUser>> MockUserManager()
        {
            return new Mock<UserManager<IdentityUser>>(
                new Mock<IUserStore<IdentityUser>>().Object,
                null, null, null, null, null, null, null, null
            );
        }

        private Mock<SignInManager<IdentityUser>> MockSignInManager()
        {
            return new Mock<SignInManager<IdentityUser>>(
                _userManager.Object,
                new HttpContextAccessor(),
                new Mock<IUserClaimsPrincipalFactory<IdentityUser>>().Object,
                null, null, null, null
            );
        }
    }
}
