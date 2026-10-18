using BenchCodeMainService.Entities;
using BenchCodeMainService.Services;
using Microsoft.AspNetCore.StaticFiles;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

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

app.MapGet("/competitions", (CompetitionService service) =>
    Results.Ok(service.Competitions));
app.MapGet("/competitions/{id:guid}", (Guid id, CompetitionService service) => 
    service.GetCompetition(id) switch
    {
        null => Results.NotFound(new { message = "Соревнование не найдено." }),
        var competition => Results.Ok(competition)
    });

app.MapGet("/competitions/{id:guid}/tasks", (Guid id, CompetitionService service) =>
    service.GetCompetition(id) switch
    {
        null => Results.NotFound(new { message = "Соревнование не найдено." }),
        var competition => Results.Ok(competition.TasksIds.Join(service.Tasks,
                                                                competitionTaskId => competitionTaskId,
                                                                task => task.Id,
                                                                (_, task) => task))
    });
app.MapGet("/competitions/{id:guid}/tasks/{taskId:guid}", (Guid id, Guid taskId, CompetitionService service) =>
    service.GetCompetition(id) switch
    {
        null => Results.NotFound(new { message = "Соревнование не найдено." }),
        var competition => service.Tasks.FirstOrDefault(task => task.Id == taskId) switch
        {
            null => Results.NotFound(new { message = "Задача не найдена." }),
            var task => Results.Ok(task)
        }
    });



app.MapPost("/competitions/{id:guid}/tasks/{taskId:guid}/submissions", (Guid id, Guid taskId, Submission submission, CompetitionService service) =>
{
    return service.Submit(taskId, submission);
});
app.MapGet("/competitions/{id:guid}/tasks/{taskId:guid}/submissions", (Guid id, Guid taskId, CompetitionService service) =>
{
    return service.GetSubmissions(taskId);
});
app.MapGet("/competitions/{id:guid}/tasks/{taskId:guid}/submissions/{submissionId:guid}", (Guid id, Guid taskId, Guid submissionId, CompetitionService service) =>
{
    var submissions = service.GetSubmissions(taskId);
    var submission = submissions.FirstOrDefault(s => s.Id == submissionId);
    if (submission is null)
        return Results.NotFound(new { message = "Решение не найдено." });
    return Results.Ok(submission);
});

app.MapGet("/competitions/{id:guid}/check-password", (Guid id, string password, CompetitionService service) =>
{
    var competition = service.GetCompetition(id);
    if (competition is null)
        return Results.NotFound(new { message = "Соревнование не найдено." });
    if (competition.Password != password)
        return Results.Unauthorized();
    return Results.Ok(new { message = "Пароль верный." });
});

app.MapFallbackToFile("index.html");

app.Run();