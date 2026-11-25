using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(useHttpResilience: false);

builder.Services.AddHttpClient("auth", client => client.BaseAddress = new Uri("https://auth"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapPost("/api/users/register", ForwardAuthRequest);
app.MapPost("/connect/token", ForwardAuthRequest);
app.MapFallbackToFile("index.html");
app.MapDefaultEndpoints();

app.Run();

static async Task ForwardAuthRequest(HttpContext context, IHttpClientFactory clients)
{
    if (!context.Request.IsHttps)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var path = context.Request.Path.Value!;
    using var request = new HttpRequestMessage(HttpMethod.Post, path)
    {
        Content = new StreamContent(context.Request.Body)
    };
    if (context.Request.ContentType is { } contentType)
    {
        request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
    }

    using var client = clients.CreateClient("auth");
    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
        context.RequestAborted);
    context.Response.StatusCode = (int)response.StatusCode;
    context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
    context.Response.Headers.CacheControl = "no-store";
    await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
}