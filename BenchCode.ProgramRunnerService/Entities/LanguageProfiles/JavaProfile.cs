namespace BenchCode.ProgramRunnerService.Entities.LanguageProfiles;

public class JavaProfile : LanguageProfile
{
    public override string Id => "java";
    public override string SourcePath => "Main.java";
    public override string ArtifactPath => "/workspace/Main.jar";

    protected override StageOptions BuildOptions => new(
        Image: "eclipse-temurin:21-jdk-alpine",
        Cmd:
        [
            "sh",
            "-c",
            "javac -encoding UTF-8 Main.java && jar cfe Main.jar Main *.class"
        ],
        Limits: new(1536L << 20, 2_000_000_000, 256, TimeSpan.FromSeconds(30), 128L << 20)
    );

    protected override StageOptions RunOptions => new(
        Image: "eclipse-temurin:21-jre-alpine",
        Cmd:
        [
            "sh",
            "-c",
            "exec java -Xmx192m -Xss64m -jar Main.jar < input.txt",
        ],
        Limits: new(512L << 20, 1_000_000_000, 256, TimeSpan.FromSeconds(4), 32L << 20)
    );
}