using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using NetDevPack.Security.JwtSigningCredentials;
using NetDevPack.Security.JwtSigningCredentials.Interfaces;
using NSE.Identidade.API.Data;
using NSE.Identidade.API.Extensions;
using NSE.Identidade.API.Services;
using NSE.Identidade.API.Tests.Services.Fake;
using NSE.WebAPI.Core.Identidade;
using NSE.WebAPI.Core.Usuario;
using System;

namespace NSE.Identidade.API.Tests.Controllers
{
    public abstract class AuthControllerTestsBase
    {
        protected readonly AuthenticationService AuthService;
        protected readonly ApplicationDbContext DbContext;
        protected readonly Mock<UserManager<IdentityUser>> UserManagerMock;
        protected readonly Mock<SignInManager<IdentityUser>> SignInManagerMock;
        protected readonly Mock<IJsonWebKeySetService> JwksMock;
        protected readonly Mock<IAspNetUser> AspNetUserMock;

        protected AuthControllerTestsBase()
        {
            // EF InMemory
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            DbContext = new ApplicationDbContext(options);

            // UserManager Fake
            UserManagerMock = new Mock<UserManager<IdentityUser>>(
                new Mock<IUserStore<IdentityUser>>().Object,
                null, null, null, null, null, null, null, null
            );

            // SignInManager Fake
            SignInManagerMock = new Mock<SignInManager<IdentityUser>>(
                UserManagerMock.Object,
                new HttpContextAccessor(),
                new Mock<IUserClaimsPrincipalFactory<IdentityUser>>().Object,
                null, null, null, null
            );

            // JWKS Fake
            JwksMock = new Mock<IJsonWebKeySetService>();
            var jwksReal = new JsonWebKeySetServiceFake();
            JwksMock
                .Setup(x => x.GetCurrent(It.IsAny<JwksOptions>()))
                .Returns(() => jwksReal.GetCurrent());

            // IAspNetUser Fake
            AspNetUserMock = new Mock<IAspNetUser>();
            AspNetUserMock.Setup(x => x.ObterHttpContext()).Returns(new DefaultHttpContext());

            // Options
            var appSettings = Options.Create(new AppSettings());
            var tokenSettings = Options.Create(new AppTokenSettings { RefreshTokenExpiration = 2 });

            // AuthenticationService real
            AuthService = new AuthenticationService(
                SignInManagerMock.Object,
                UserManagerMock.Object,
                appSettings,
                tokenSettings,
                DbContext,
                JwksMock.Object,
                AspNetUserMock.Object
            );
        }
    }
}
