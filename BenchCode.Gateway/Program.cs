using System.Threading.RateLimiting;
using BenchCode.Shared;
using OpenIddict.Abstractions;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

var issuer = builder.Configuration["OpenIddict:Issuer"];
if (!Uri.TryCreate(issuer, UriKind.Absolute, out var issuerUri))
    throw new InvalidOperationException("OpenIddict:Issuer must be configured as an absolute URI.");

builder.Services.AddOpenIddictTokenValidation(issuerUri, "benchcode-api");
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("gateway-user", policy =>
        policy.RequireAuthenticatedUser().RequireClaim(OpenIddictConstants.Claims.Subject));

builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        return CreateIpLimiter(httpContext, 100, TimeSpan.FromMinutes(1));
    });
    opt.AddPolicy("auth", httpContext =>
    {
        return CreateIpLimiter(httpContext, 10, TimeSpan.FromMinutes(1));
    });
});

var routes = new[]
{
    new RouteConfig
    {
        RouteId = "auth",
        ClusterId = "auth-cluster",
        RateLimiterPolicy = "auth",
        Match = new RouteMatch
        {
            Path = "/auth/{**catch-all}"
        },
        Transforms =
        [
            new Dictionary<string, string> { ["PathRemovePrefix"] = "/auth" }
        ]
    },
    new RouteConfig
    {
        RouteId = "competitions",
        ClusterId = "competition-cluster",
        AuthorizationPolicy = "gateway-user",
        Match = new RouteMatch
        {
            Path = "/api/{**catch-all}"
        },
        Transforms =
        [
            new Dictionary<string, string> { ["PathPattern"] = "/internal/{**catch-all}" }
        ]
    },
};

var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "auth-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            {
                "auth-destination",
                new DestinationConfig
                {
                    Address = builder.Configuration["ReverseProxy:AuthAddress"] ?? "https+http://auth/"
                }
            }
        }
    },
    new ClusterConfig
    {
        ClusterId = "competition-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            {
                "competition-destination",
                new DestinationConfig
                {
                    Address = builder.Configuration["ReverseProxy:CompetitionAddress"] ?? "https+http://competition/"
                }
            }
        }
    },
};

// Регистрируем сервис обратного прокси и загружаем конфиг из настроек
builder.Services.AddReverseProxy()
    .AddServiceDiscoveryDestinationResolver()
    .LoadFromMemory(routes, clusters);

var app = builder.Build();


app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/auth") && context.Request.HasFormContentType)
        context.Request.EnableBuffering();

    await next();
});
// проверка токена
app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/auth") &&
        context.Request.HasFormContentType &&
        context.Request.Body.CanSeek)
    {
        context.Request.Body.Position = 0;
    }

    if (context.Request.Path.StartsWithSegments("/api"))
    {
        // отбрасывается bearer токен
        context.Request.Headers.Remove("Authorization");
        // удаляется внутренний заголовок (если был в запросе)
        context.Request.Headers.Remove(InternalRequestHeaders.UserId);
        // получается id
        var userId = context.User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value;
        // id записывается во внутренний заголовок X-BenchCode-User-Id
        if (!string.IsNullOrWhiteSpace(userId))
            context.Request.Headers[InternalRequestHeaders.UserId] = userId;
    }

    await next();
});

app.MapReverseProxy();
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");
app.MapDefaultEndpoints();

app.Run();

static RateLimitPartition<string> CreateIpLimiter(HttpContext httpContext, int permitLimit, TimeSpan window)
{
    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    return RateLimitPartition.GetFixedWindowLimiter(ipAddress, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 0
    });
}