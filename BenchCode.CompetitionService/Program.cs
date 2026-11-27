using BenchCode.CompetitionService;

var builder = WebApplication.CreateBuilder(args);

builder.AddNpgsqlDbContext<CompetitionDbContext>(connectionName: "competitionsdb");
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