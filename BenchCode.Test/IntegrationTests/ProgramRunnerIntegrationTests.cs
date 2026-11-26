using BenchCode.ProgramRunnerService.Entities;
using BenchCode.ProgramRunnerService.Entities.LanguageProfiles;
using BenchCode.Shared.Entities;
using Docker.DotNet;

namespace BenchCode.Test.IntegrationTests;

public sealed class ProgramRunnerIntegrationTests
{
    public static TheoryData<string, string> RunnerPrograms => new()
    {
        {
            "cpp",
            """
            #include <iostream>
            #include <string>

            int main() {
                std::string name;
                std::getline(std::cin, name);
                std::cout << "Hello " << name << '\n';
            }
            """
        },
        {
            "java",
            """
            import java.util.Scanner;

            public class Main {
                public static void main(String[] args) {
                    try (var input = new Scanner(System.in)) {
                        System.out.println("Hello " + input.nextLine());
                    }
                }
            }
            """
        },
        {
            "csharp",
            """
            var name = Console.ReadLine();
            Console.WriteLine($"Hello {name}");
            """
        }
    };

    public Verdict Verdict { get; private set; }

    [Theory]
    [MemberData(nameof(RunnerPrograms))]
    [Trait("Category", "DockerIntegration")]
    public async Task Runner_BuildsAndRunsProgram(string language, string source)
    {
        using var docker = new DockerClientBuilder().Build();
        var runner = new ProgramRunner(docker);
        var profile = CreateProfile(language);

        var build = await runner.BuildAsync(profile, source);

        Assert.True(build.Success, $"{language} compilation failed: {build.Error}");
        Assert.NotNull(build.Artifact);

        var result = await runner.RunAsync(
            profile,
            build.Artifact,
            new TestCase("world\n", "Hello world\n"));

        Assert.Equal(Verdict.OK, result.Verdict);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal("Hello world\n", result.Stdout);
    }

    private static LanguageProfile CreateProfile(string language) => language switch
    {
        "cpp" => new CppProfile(),
        "java" => new JavaProfile(),
        "csharp" => new CSharpProfile(),
        _ => throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language")
    };
}
