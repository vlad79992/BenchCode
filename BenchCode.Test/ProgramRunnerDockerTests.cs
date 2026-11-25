extern alias RunnerService;

using Docker.DotNet;
using RunnerEntities = RunnerService::BenchCode.ProgramRunnerService.Entities;

namespace BenchCode.Test;

public sealed class ProgramRunnerDockerTests
{
    [Fact]
    [Trait("Category", "Docker")]
    public async Task CppProfile_BuildsAndRunsProgram()
    {
        using var docker = new DockerClientBuilder().Build();
        var runner = new RunnerEntities.ProgramRunner(docker);
        var images = await docker.Images.ListImagesAsync(new Docker.DotNet.Models.ImagesListParameters());
        var hasBusyboxRunner = images.Any(image =>
            image.RepoTags?.Contains("busybox:1.38.0-glibc", StringComparer.Ordinal) == true);
        RunnerEntities.LanguageProfile profile = hasBusyboxRunner
            ? new RunnerEntities.CppProfile()
            : new CppProfileWithLocalRuntime();

        var invalidBuild = await runner.BuildAsync(profile, "int main( {");
        Assert.False(invalidBuild.Success);
        Assert.False(string.IsNullOrWhiteSpace(invalidBuild.Error));

        var build = await runner.BuildAsync(
            profile,
            """
            #include <iostream>
            int main() {
                int value;
                std::cin >> value;
                std::cout << value * 2 << '\n';
            }
            """);
        Assert.True(build.Success, build.Error);

        var accepted = await runner.RunAsync(
            profile,
            build.Artifact!,
            new RunnerEntities.TestCase("21", "42"));
        var wrongAnswer = await runner.RunAsync(
            profile,
            build.Artifact!,
            new RunnerEntities.TestCase("21", "41"));

        Assert.Equal(RunnerEntities.Verdict.AC, accepted.Verdict);
        Assert.Equal("42\n", accepted.Stdout);
        Assert.Equal(RunnerEntities.Verdict.WA, wrongAnswer.Verdict);
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task RunAsync_CustomCheckerAcceptsAlternateAnswerAndRejectsInvalidAnswer()
    {
        using var docker = new DockerClientBuilder().Build();
        var runner = new RunnerEntities.ProgramRunner(docker);
        var profile = new RunnerEntities.JavaProfile();

        var solutionBuild = await runner.BuildAsync(
            profile,
            """
            import java.util.*;
            public class Main {
                public static void main(String[] args) {
                    int target = new Scanner(System.in).nextInt();
                    if (target == 99) throw new RuntimeException();
                    if (target == 77) while (true) {}
                    if (target == 88) {
                        System.out.print("x".repeat(70000));
                        return;
                    }
                    System.out.println("3 7");
                }
            }
            """);
        Assert.True(solutionBuild.Success, solutionBuild.Error);

        var checkerBuild = await runner.BuildAsync(
            profile,
            """
            import java.nio.file.*;
            import java.util.*;
            public class Main {
                public static void main(String[] args) throws Exception {
                    Scanner input = new Scanner(Files.readString(Path.of("input.txt")));
                    Scanner output = new Scanner(Files.readString(Path.of("actual.txt")));
                    String expected = Files.readString(Path.of("expected.txt")).trim();
                    if (expected.equals("system error")) System.exit(2);
                    if (!input.hasNextInt() || !output.hasNextInt()) System.exit(1);
                    int target = input.nextInt();
                    int first = output.nextInt();
                    if (!output.hasNextInt()) System.exit(1);
                    int second = output.nextInt();
                    System.exit(!output.hasNext() && first + second == target ? 0 : 1);
                }
            }
            """);
        Assert.True(checkerBuild.Success, checkerBuild.Error);

        var accepted = await runner.RunAsync(
            profile,
            solutionBuild.Artifact!,
            new RunnerEntities.TestCase("10", "0 10"),
            CancellationToken.None,
            profile,
            checkerBuild.Artifact);
        var rejected = await runner.RunAsync(
            profile,
            solutionBuild.Artifact!,
            new RunnerEntities.TestCase("11", "0 11"),
            CancellationToken.None,
            profile,
            checkerBuild.Artifact);
        var checkerError = await runner.RunAsync(
            profile,
            solutionBuild.Artifact!,
            new RunnerEntities.TestCase("10", "system error"),
            CancellationToken.None,
            profile,
            checkerBuild.Artifact);
        var exactMismatch = await runner.RunAsync(
            profile,
            solutionBuild.Artifact!,
            new RunnerEntities.TestCase("10", "0 10"));
        var runtimeError = await runner.RunAsync(
            profile,
            solutionBuild.Artifact!,
            new RunnerEntities.TestCase("99", "0 99"));
        var outputLimit = await runner.RunAsync(
            profile,
            solutionBuild.Artifact!,
            new RunnerEntities.TestCase("88", string.Empty));
        var timeLimit = await runner.RunAsync(
            profile,
            solutionBuild.Artifact!,
            new RunnerEntities.TestCase("77", string.Empty));

        Assert.Equal(RunnerEntities.Verdict.AC, accepted.Verdict);
        Assert.Equal("3 7\n", accepted.Stdout);
        Assert.Equal(RunnerEntities.Verdict.WA, rejected.Verdict);
        Assert.Equal(RunnerEntities.Verdict.SE, checkerError.Verdict);
        Assert.Equal(RunnerEntities.Verdict.WA, exactMismatch.Verdict);
        Assert.Equal(RunnerEntities.Verdict.RE, runtimeError.Verdict);
        Assert.Equal(RunnerEntities.Verdict.OLE, outputLimit.Verdict);
        Assert.Equal(RunnerEntities.Verdict.TLE, timeLimit.Verdict);
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task CSharpProfile_BuildsAndRunsProgram()
    {
        using var docker = new DockerClientBuilder().Build();
        var runner = new RunnerEntities.ProgramRunner(docker);
        var profile = new RunnerEntities.CSharpProfile();

        var invalidBuild = await runner.BuildAsync(profile, "public class {");
        Assert.False(invalidBuild.Success);
        Assert.False(string.IsNullOrWhiteSpace(invalidBuild.Error));

        var build = await runner.BuildAsync(profile, "Console.WriteLine(\"ready\");");
        Assert.True(build.Success, build.Error);

        var result = await runner.RunAsync(
            profile,
            build.Artifact!,
            new RunnerEntities.TestCase(string.Empty, "ready"));

        Assert.Equal(RunnerEntities.Verdict.AC, result.Verdict);
        Assert.Equal("ready\n", result.Stdout);
    }

    private sealed class CppProfileWithLocalRuntime : RunnerEntities.CppProfile
    {
        protected override RunnerEntities.LanguageProfile.StageOptions RunOptions => new(
            Image: "mcr.microsoft.com/dotnet/runtime:11.0",
            Cmd: ["sh", "-c", "exec ./main < input.txt"],
            Limits: new(256L << 20, 1_000_000_000, 256, TimeSpan.FromSeconds(2), 32L << 20));
    }
}
