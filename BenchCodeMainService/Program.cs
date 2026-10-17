using BenchCodeMainService.Entities.Dtos;
using BenchCodeMainService.Services;
using Microsoft.AspNetCore.StaticFiles;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using BenchCodeMainService.Entities;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSingleton<CompetitionService>();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

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

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".mjs"] = "application/javascript";
provider.Mappings[".wasm"] = "application/wasm";
provider.Mappings[".js"] = "application/javascript";

app.UseDefaultFiles();

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name.EndsWith(".wasm"))
            ctx.Context.Response.Headers.ContentType = "application/wasm";
        else if (ctx.File.Name.EndsWith(".mjs") || ctx.File.Name.EndsWith(".js"))
        {
            ctx.Context.Response.Headers.ContentType = "application/javascript";
        }
    }
});

app.UseRouting();
app.MapControllers();

app.MapGet("/competitions", (int? userId, CompetitionService service) =>
{
    if (userId is not null && service.GetUser(userId.Value) is null)
        return Results.NotFound(new { message = "Студент не найден." });

    return Results.Ok(service.GetCompetitions(userId));
});

app.MapGet("/competitions/{id:int}", (int id, int? userId, CompetitionService service) =>
{
    var competition = service.GetCompetition(id);
    if (competition is null)
        return Results.NotFound(new { message = "Соревнование не найдено." });

    if (userId is not null)
    {
        var user = service.GetUser(userId.Value);
        if (user is null)
            return Results.NotFound(new { message = "Студент не найден." });
        if (!competition.GroupIds.Contains(user.GroupId))
            return Results.Problem(
                title: "Соревнование недоступно вашей группе.",
                statusCode: StatusCodes.Status403Forbidden);
    }

    return Results.Ok(competition);
});

app.MapGet("/groups", (CompetitionService service) => Results.Ok(service.GetGroups()));
app.MapGet("/users", (CompetitionService service) => Results.Ok(service.GetUsers()));
app.MapGet("/users/{id:int}", (int id, CompetitionService service) =>
    service.GetUser(id) is User user ? Results.Ok(user) : Results.NotFound());
app.MapGet("/users/{userId:int}/submissions", (int userId, CompetitionService service) =>
    service.GetUser(userId) is null
        ? Results.NotFound(new { message = "Студент не найден." })
        : Results.Ok(service.GetSubmissions(userId)));
app.MapPost("/tasks/{taskId:int}/submissions", (int taskId, SubmitDto request, CompetitionService service) =>
{
    var result = service.Submit(taskId, request);
    return result.Error is not null
        ? Results.Problem(
            title: result.Error,
            statusCode: result.StatusCode)
        : Results.Created($"/users/{result.Submission!.UserId}/submissions", result.Submission);
});

app.MapFallbackToFile("index.html");

app.Run();