namespace BenchCode.ProgramRunnerService.Entities.LanguageProfiles;

public class CppProfile : LanguageProfile
{
    public override string Id => "cpp";
    public override string SourcePath => "main.cpp";

    protected override StageOptions BuildOptions => new(
        Image: "n0madic/alpine-gcc:14.3.0",
        Cmd:
        [
            "g++",
            "-std=c++20",
            "-O2",
            "-pipe",
            "-static",
            "main.cpp",
            "-o",
            "main"
        ],
        Limits: new(1536L << 20, 2_000_000_000, 256, TimeSpan.FromSeconds(30), 128L << 20)
    );

    protected override StageOptions RunOptions => new(
        Image: "busybox:1.38.0-glibc",
        Cmd:
        [
            "sh",
            "-c",
            "exec ./main < input.txt",
        ],
        Limits: new(256L << 20, 1_000_000_000, 256, TimeSpan.FromSeconds(2), 32L << 20)
    );
}
