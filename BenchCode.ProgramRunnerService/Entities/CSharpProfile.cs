namespace BenchCode.ProgramRunnerService.Entities;

public class CSharpProfile : LanguageProfile
{
    public override string Id => "csharp";
    public override string SourcePath => "Program.cs";
    public override string ArtifactPath => "/workspace/artifact.tar";

    protected override LanguageProfile.StageOptions BuildOptions => new(
        Image: "mcr.microsoft.com/dotnet/sdk:11.0",
        Cmd: [
            "sh",
            "-c",
            "cat << 'EOF' > App.csproj\n" +
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n" +
            "  <PropertyGroup>\n" +
            "    <OutputType>Exe</OutputType>\n" +
            "    <TargetFramework>net11.0</TargetFramework>\n" +
            "    <ImplicitUsings>enable</ImplicitUsings>\n" +
            "    <Nullable>enable</Nullable>\n" +
            "  </PropertyGroup>\n" +
            "</Project>\n" +
            "EOF\n" +
            "dotnet publish App.csproj -c Release -o out --nologo && " +
            "tar -cf artifact.tar -C out ."
        ],
        Limits: new(
            1536L << 20,
            2_000_000_000,
            256,
            TimeSpan.FromSeconds(30),
            256L << 20
        )
    );

    protected override LanguageProfile.StageOptions RunOptions => new(
        Image: "mcr.microsoft.com/dotnet/runtime:11.0",
        Cmd: ["sh", "-c", "tar -xf artifact.tar -C /workspace && exec dotnet App.dll < input.txt"],
        Limits: new(
            512L << 20,
            1_000_000_000,
            256,
            TimeSpan.FromSeconds(3),
            32L << 20
        )
    );
}