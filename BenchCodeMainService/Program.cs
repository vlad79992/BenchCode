using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
    await next();
});

app.UseDefaultFiles();

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".wasm"))
            ctx.Context.Response.Headers.ContentType = "application/wasm";
        if (ctx.File.Name.EndsWith(".mjs"))
            ctx.Context.Response.Headers.ContentType = "application/javascript";
    }
});

app.UseRouting();
app.MapControllers();

//app.MapGet("/", () => "Hello World!");
app.MapFallbackToFile("index.html");

app.Run();
