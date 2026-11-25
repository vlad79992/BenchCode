using Microsoft.AspNetCore.Hosting;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(useHttpResilience: false);

AddProxyClient("auth");
AddProxyClient("competition");

void AddProxyClient(string service) => builder.Services
    .AddHttpClient(service, client => client.BaseAddress = new Uri($"https://{service}"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    });

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapPost("/api/users/register", (HttpContext context, IHttpClientFactory clients) =>
    ForwardRequest(context, clients, "auth"));
app.MapPost("/connect/token", (HttpContext context, IHttpClientFactory clients) =>
    ForwardRequest(context, clients, "auth"));

var competitions = app.MapGroup("/api/competitions");
competitions.MapPost("/join", ForwardCompetitionRequest);
competitions.MapGet("/{id}", ForwardCompetitionRequest);
competitions.MapGet("/{id}/tasks", ForwardCompetitionRequest);
competitions.MapGet("/{id}/tasks/{taskId:int}", ForwardCompetitionRequest);
competitions.MapPost("/{id}/tasks/{taskId:int}/submit", ForwardCompetitionRequest);
competitions.MapGet("/{id}/tasks/{taskId:int}/submissions/{submissionId:int}", ForwardCompetitionRequest);
app.MapFallbackToFile("index.html");
app.MapDefaultEndpoints();

app.Run();

static Task ForwardCompetitionRequest(HttpContext context, IHttpClientFactory clients) =>
    ForwardRequest(context, clients, "competition");

static async Task ForwardRequest(HttpContext context, IHttpClientFactory clients, string service)
{
    if (!context.Request.IsHttps)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    var path = context.Request.Path.Value!;
    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), path);
    if (HttpMethods.IsPost(context.Request.Method))
    {
        request.Content = new StreamContent(context.Request.Body);
        if (context.Request.ContentType is { } contentType)
        {
            request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }
    }
    if (service == "competition" && context.Request.Headers.Authorization is { Count: 1 } authorization)
    {
        request.Headers.TryAddWithoutValidation("Authorization", authorization.ToString());
    }

    using var client = clients.CreateClient(service);
    using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead,
        context.RequestAborted);
    context.Response.StatusCode = (int)response.StatusCode;
    context.Response.ContentType = response.Content.Headers.ContentType?.ToString();
    context.Response.Headers.CacheControl = "no-store";
    await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
}