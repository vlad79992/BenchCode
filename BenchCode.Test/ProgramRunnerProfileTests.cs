extern alias RunnerService;

using Docker.DotNet.Models;
using RunnerEntities = RunnerService::BenchCode.ProgramRunnerService.Entities;

namespace BenchCode.Test;

public sealed class ProgramRunnerProfileTests
{
    [Fact]
    public void CppProfile_ConfiguresCompilerAndRuntime()
    {
        var profile = new RunnerEntities.CppProfile();

        Assert.Equal("cpp", profile.Id);
        Assert.Equal("main.cpp", profile.SourcePath);
        Assert.Equal("/workspace/main", profile.ArtifactPath);
        Assert.Equal(TimeSpan.FromSeconds(30), profile.BuildTimeout);
        Assert.Equal(TimeSpan.FromSeconds(2), profile.RunTimeout);
        Assert.Collection(
            profile.RequiredImages,
            image => Assert.Equal("n0madic/alpine-gcc:14.3.0", image),
            image => Assert.Equal("busybox:1.38.0-glibc", image));

        AssertSandbox(profile.BuildCompilerContainer());
        AssertSandbox(profile.BuildRunnerContainer());
    }

    [Fact]
    public void JavaProfile_ConfiguresCompilerAndRuntime()
    {
        var profile = new RunnerEntities.JavaProfile();

        Assert.Equal("java", profile.Id);
        Assert.Equal("Main.java", profile.SourcePath);
        Assert.Equal("/workspace/Main.jar", profile.ArtifactPath);
        Assert.Equal(TimeSpan.FromSeconds(30), profile.BuildTimeout);
        Assert.Equal(TimeSpan.FromSeconds(4), profile.RunTimeout);
        Assert.Collection(
            profile.RequiredImages,
            image => Assert.Equal("eclipse-temurin:21-jdk-alpine", image),
            image => Assert.Equal("eclipse-temurin:21-jre-alpine", image));

        AssertSandbox(profile.BuildCompilerContainer());
        AssertSandbox(profile.BuildRunnerContainer());
    }

    [Fact]
    public void CSharpProfile_ConfiguresCompilerAndRuntime()
    {
        var profile = new RunnerEntities.CSharpProfile();

        Assert.Equal("csharp", profile.Id);
        Assert.Equal("Program.cs", profile.SourcePath);
        Assert.Equal("/workspace/artifact.tar", profile.ArtifactPath);
        Assert.Equal(TimeSpan.FromSeconds(30), profile.BuildTimeout);
        Assert.Equal(TimeSpan.FromSeconds(3), profile.RunTimeout);
        Assert.Collection(
            profile.RequiredImages,
            image => Assert.Equal("mcr.microsoft.com/dotnet/sdk:11.0", image),
            image => Assert.Equal("mcr.microsoft.com/dotnet/runtime:11.0", image));

        AssertSandbox(profile.BuildCompilerContainer());
        AssertSandbox(profile.BuildRunnerContainer());
    }

    private static void AssertSandbox(CreateContainerParameters container)
    {
        var hostConfig = Assert.IsType<HostConfig>(container.HostConfig);
        var capabilities = hostConfig.CapDrop
            ?? throw new InvalidOperationException("Container capabilities were not configured.");
        var securityOptions = hostConfig.SecurityOpt
            ?? throw new InvalidOperationException("Container security options were not configured.");

        Assert.Equal("/workspace", container.WorkingDir);
        Assert.Equal("65534:65534", container.User);
        Assert.False(container.Tty);
        Assert.True(container.NetworkDisabled);
        Assert.Equal("none", hostConfig.NetworkMode);
        Assert.Contains("ALL", capabilities);
        Assert.Contains("no-new-privileges:true", securityOptions);
        Assert.Equal(hostConfig.Memory, hostConfig.MemorySwap);
        Assert.True(hostConfig.Memory > 0);
        Assert.True(hostConfig.NanoCPUs > 0);
        Assert.True(hostConfig.PidsLimit > 0);
    }
}
