using BenchCode.CompetitionService;
using BenchCode.CompetitionService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.ServiceDiscovery;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<CompetitionDbContext>(connectionName: "competitionsdb");
builder.AddRabbitMQClient(connectionName: "rabbitmq");
builder.Services.Configure<SubmissionQueueOptions>(
    builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddHttpClient<ISubmissionRunner, RunnerClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Runner:Address"]!);
}).AddServiceDiscovery();
builder.Services.AddSingleton<ISubmissionQueue, SubmissionQueueClient>();
builder.Services.AddHostedService<FastCheckQueueWorker>();
builder.Services.AddHostedService<FullSubmissionQueueWorker>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();