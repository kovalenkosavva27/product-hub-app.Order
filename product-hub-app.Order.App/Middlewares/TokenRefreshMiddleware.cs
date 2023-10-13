using IdentityModel.Client;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;

namespace product_hub_app.Order.App.Middlewares
{
    public class TokenRefreshMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public TokenRefreshMiddleware(RequestDelegate next, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _next = next;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            IConfiguration keycloakConfig = _configuration.GetSection("Keycloak");
            // Проверьте текущий токен
            var currentToken = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(currentToken);

            if (token.ValidTo.Subtract(DateTime.UtcNow) < TimeSpan.FromMinutes(1))
            {
                var httpClient = _httpClientFactory.CreateClient();
                var tokenResponse = await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
                {
                    Address = ($"{keycloakConfig["auth-server-url"]}realms/{keycloakConfig["realm"]}/protocol/openid-connect/token"),
                    ClientId = keycloakConfig["resource"],
                    ClientSecret = keycloakConfig["secret"],
                    RefreshToken = token.RawSignature
                }) ;

                if (!tokenResponse.IsError)
                {
                    var newAccessToken = tokenResponse.AccessToken;
                    context.Request.Headers["Authorization"] = "Bearer " + newAccessToken;
                }
            }
            await _next(context);
        }
    }

}
