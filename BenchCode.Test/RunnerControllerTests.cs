extern alias RunnerService;

using Docker.DotNet;
using Microsoft.AspNetCore.Mvc;
using RunnerControllers = RunnerService::BenchCode.ProgramRunnerService.Controllers;
using RunnerEntities = RunnerService::BenchCode.ProgramRunnerService.Entities;

namespace BenchCode.Test;

public sealed class RunnerControllerTests
{
    [Fact]
    public async Task RunCode_WithUnsupportedLanguage_ReturnsBadRequest()
    {
        using var docker = new DockerClientBuilder().Build();
        var controller = new RunnerControllers.RunnerController(new RunnerEntities.ProgramRunner(docker));

        var result = await controller.RunCode(CreateRequest(language: "python"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task RunCode_RequiresCheckerCodeAndLanguageTogether()
    {
        using var docker = new DockerClientBuilder().Build();
        var controller = new RunnerControllers.RunnerController(new RunnerEntities.ProgramRunner(docker));
        var request = CreateRequest(language: "java") with { CheckerCode = "class Main {}" };

        var result = await controller.RunCode(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task RunCode_RejectsUnsupportedCheckerLanguage()
    {
        using var docker = new DockerClientBuilder().Build();
        var controller = new RunnerControllers.RunnerController(new RunnerEntities.ProgramRunner(docker));
        var request = CreateRequest(language: "java") with
        {
            CheckerLanguage = "python",
            CheckerCode = "class Main {}",
        };

        var result = await controller.RunCode(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task RunCode_RejectsBlankCheckerSource()
    {
        using var docker = new DockerClientBuilder().Build();
        var controller = new RunnerControllers.RunnerController(new RunnerEntities.ProgramRunner(docker));
        var request = CreateRequest(language: "java") with
        {
            CheckerLanguage = "java",
            CheckerCode = " ",
        };

        var result = await controller.RunCode(request, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static RunnerControllers.RunnerRequest CreateRequest(string language) => new()
    {
        Language = language,
        Code = "class Main {}",
        Tests = [new RunnerControllers.RunnerTestRequest()],
    };
}
