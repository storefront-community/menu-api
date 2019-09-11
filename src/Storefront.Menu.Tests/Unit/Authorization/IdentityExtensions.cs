using System.IdentityModel.Tokens.Jwt;
using Storefront.Menu.API.Authorization;
using Storefront.Menu.Tests.Fakes;
using Xunit;

namespace Storefront.Menu.Tests.Unit.Authorization
{
    public sealed class IdentityExtensions
    {
        private readonly ApiServer _server;

        public IdentityExtensions()
        {
            _server = new ApiServer();
        }

        [Fact]
        public void ShouldGetIdFromClaims()
        {
            var token = new ApiToken(_server.JwtOptions)
            {
                TenantId = 20,
                UserId = 50
            }
            .ToString();

            var user = new JwtSecurityTokenHandler().ReadJwtToken(token);

            Assert.Equal(20, user.Claims.TenantId());
            Assert.Equal(50, user.Claims.UserId());
        }
    }
}