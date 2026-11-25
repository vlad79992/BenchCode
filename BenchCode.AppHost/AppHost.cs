using Microsoft.Extensions.Hosting;
using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);
var repositoryRoot = Path.GetFullPath(
    Path.Combine(builder.Environment.ContentRootPath, ".."));
var composeWebAppDirectory = Path.Combine(repositoryRoot, "BenchCode.ComposeWebApp");
var webRootDirectory = Path.Combine(repositoryRoot, "BenchCode.Web", "wwwroot");
var composeWasmVariant = builder.Configuration["Compose:Variant"]
                             ?.Trim()
                             .ToLowerInvariant()
                         ?? (builder.Environment.IsDevelopment() ? "debug" : "release");

if (composeWasmVariant is not ("debug" or "release"))
{
    throw new InvalidOperationException(
        $"Unsupported Compose:Variant '{composeWasmVariant}'. Use 'debug' or 'release'.");
}

var composeWasmBuild = builder.AddExecutable(
    "compose-wasm-build",
    "docker",
    repositoryRoot,
    [
        "buildx",
        "build",
        "--progress=plain",
        "--file",
        Path.Combine(composeWebAppDirectory, "Dockerfile"),
        "--target",
        "frontend-export",
        "--build-arg",
        $"COMPOSE_WASM_VARIANT={composeWasmVariant}",
        "--output",
        $"type=local,dest={webRootDirectory}",
        composeWebAppDirectory
    ]);

var postgres = builder.AddPostgres("postgres")
                      .WithDataVolume();

var rabbitmq = builder.AddRabbitMQ("rabbitmq");

//builder.AddProject<Projects.BenchCode_Web>("web")
builder.AddDotnetProject("web", "../BenchCode.Web/BenchCode.Web.csproj")
    .WaitForCompletion(composeWasmBuild);

var authDb = postgres.AddDatabase("authdb");
//var authService = builder.AddProject<Projects.BenchCode_AuthService>("auth")
var authService = builder.AddDotnetProject("auth", "../BenchCode.AuthService/BenchCode.AuthService.csproj")
    .WaitFor(authDb)
    .WithReference(authDb)
    .WithEnvironment("OpenIddict__Issuer", "https://localhost:7231/");

var competitionsDb = postgres.AddDatabase("competitionsdb");
var solutionsDb = postgres.AddDatabase("solutionsdb");

var competitionService = builder.AddDotnetProject("competition", "../BenchCode.CompetitionService/BenchCode.CompetitionService.csproj")
    .WaitFor(competitionsDb)
    .WithReference(competitionsDb)
    .WithEnvironment("OpenIddict__Issuer", "https://localhost:7231/");

//var programRunnerService = builder.AddProject<Projects.BenchCode_ProgramRunnerService>("program-runner");
var programRunnerService = builder.AddDotnetProject("programRunner", "../BenchCode.ProgramRunnerService/BenchCode.ProgramRunnerService.csproj")
    .WaitFor(competitionsDb)
    .WaitFor(solutionsDb)
    .WithReference(solutionsDb);

if (builder.Environment.IsDevelopment())
{
    var scalar = builder.AddScalarApiReference(opt =>
    {
        opt.PreferHttpsEndpoint();
        opt.AllowSelfSignedCertificates();
    });

    scalar.WithApiReference(authService)
        .WithApiReference(competitionService)
        .WithApiReference(programRunnerService);
}

builder.Build().Run();