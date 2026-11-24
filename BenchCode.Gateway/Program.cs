using System.Threading.RateLimiting;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

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
};

var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "auth-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            { "auth-destination", new DestinationConfig { Address = "https+http://auth/" } }
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