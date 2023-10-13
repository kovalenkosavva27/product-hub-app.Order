using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using product_hub_app.Order.App.Middlewares;
using product_hub_app.Order.Bll;
using product_hub_app.Order.Bll.DbConfiguration;
using product_hub_app.Order.Contracts.Interfaces;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System.Diagnostics;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json")
        .Build();
IConfiguration keycloakConfig = configuration.GetSection("Keycloak");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakConfig["auth-server-url"] + "realms/" + keycloakConfig["realm"];
        options.Audience = keycloakConfig["resource"];
        options.RequireHttpsMetadata = keycloakConfig["ssl-required"] != "none"; // Проверка HTTPS
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = keycloakConfig["verify-token-audience"] == "true",
            RoleClaimType = ClaimTypes.Role
        };
    });
builder.Services.AddAuthorization(options =>
{
    //Create policy with more than one claim
    options.AddPolicy("users", policy =>
    policy.RequireAssertion(context =>
    context.User.HasClaim(c =>
            (c.Value == "User") || (c.Value == "Director"))));
});
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(configuration: configuration.GetConnectionString("Redis")));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("Redis");
    options.InstanceName = "Order_";
});
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IOrderProductService, OrderProductService>();
builder.Services.AddControllers();
builder.Services.AddDbContext<OrderDbContext>(
    options =>
    {
        var connectionString = configuration.GetConnectionString("Order");

        options.UseNpgsql(connectionString);
    });
builder.Services.AddHttpClient();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{keycloakConfig["auth-server-url"]}realms/{keycloakConfig["realm"]}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{keycloakConfig["auth-server-url"]}realms/{keycloakConfig["realm"]}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
            {
                { "openid", "OpenID" },
                { "profile", "Profile" },
            }

            }
        }

    });
    c.OperationFilter<SecurityRequirementsOperationFilter>();
}); 



var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<TokenValidationMiddleware>();
app.MapControllers();
app.MapGet("/", (ClaimsPrincipal user) =>
{
    Debug.WriteLine("Привет,"+ user.FindFirstValue(ClaimTypes.NameIdentifier));

}).RequireAuthorization().WithMetadata(new SwaggerOperationAttribute
{
    Summary = "Your summary here"
}); ;

app.Run();
