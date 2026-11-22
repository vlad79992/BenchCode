using System.Globalization;
using System.Net;
using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace BenchCode.ProgramRunnerService.Entities;

public sealed class ProgramRunner(IDockerClient docker)
{
    private const UnixFileMode File0644 = (UnixFileMode)0b110_100_100;
    private const UnixFileMode File0755 = (UnixFileMode)0b111_101_101;
    private const int OutputCap = 64 * 1024;
    private const int StderrCap = 16 * 1024;

    /// <summary>
    /// Вызывается до любых операций с контейнером, чтобы установить 
    /// </summary>
    /// <param name="profile">Профиль языка</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">Выбрасывается, если <c><paramref name="profile"/> == null</c></exception>
    public async Task EnsureImagesAsync(LanguageProfile profile, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        foreach (var image in profile.RequiredImages.Distinct(StringComparer.Ordinal))
        {
            var (fromImage, tag) = SplitImageReference(image);
            await docker.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = fromImage, Tag = tag },
                authConfig: null, new Progress<JSONMessage>(), ct);
        }
    }

    /// <summary>
    /// Компилирует код для языка из профиля <paramref name="profile"/>
    /// </summary>
    /// <param name="profile"></param>
    /// <param name="code"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">
    /// Выбрасывается если <c><paramref name="profile"/> == null</c> или <c><paramref name="code"/> == null</c>
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Выбрасывается, если контейнер не завершился с ошибкой, но не вернул архив с артефактом
    /// </exception>
    public async Task<BuildOutcome> BuildAsync(LanguageProfile profile, string code, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(code);

        var id = (await docker.Containers.CreateContainerAsync(profile.BuildCompilerContainer(), ct)).ID;
        try
        {
            await InjectAsync(id, [
                new TarFile($"workspace/{profile.SourcePath}", Encoding.UTF8.GetBytes(code), File0644),
            ], ct);

            var (status, timedOut) = await StartAndWaitAsync(id, profile.BuildTimeout, ct);

            if (timedOut) return BuildOutcome.Fail("Compilation timeout");
            if (status != 0)
            {
                var state = (await docker.Containers.InspectContainerAsync(id, ct)).State!;
                if (state.OOMKilled) return BuildOutcome.Fail("Compiler out of memory");
                var (stdout, stderr, stdoutTruncated, stderrTruncated) =
                    await ReadLogsAsync(id, OutputCap, StderrCap, ct);
                var useStdout = string.IsNullOrWhiteSpace(stderr);
                var error = useStdout ? stdout : stderr;
                if (string.IsNullOrWhiteSpace(error))
                    error = $"Compiler exited with code {status}.";
                if (useStdout ? stdoutTruncated : stderrTruncated)
                    error += "\n[compiler output truncated]";

                return BuildOutcome.Fail(error);
            }

            var archive = await docker.Containers.GetArchiveFromContainerAsync(
                id,
                new ContainerPathStatParameters { Path = profile.ArtifactPath },
                statOnly: false,
                ct);

            await using var stream = archive.Stream
                ?? throw new InvalidOperationException("Демон не вернул архив с артефактом");

            return BuildOutcome.Ok(new Artifact(await Archives.ReadSingleAsync(stream, ct)));
        }
        finally { await RemoveAsync(id); }
    }

    public async Task<RunOutcome> RunAsync(LanguageProfile profile, Artifact artifact, TestCase test, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(artifact.Bytes);
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(test.Input);
        ArgumentNullException.ThrowIfNull(test.Expected);

        var id = (await docker.Containers.CreateContainerAsync(profile.BuildRunnerContainer(), ct)).ID;
        try
        {
            var artifactName = Path.GetFileName(profile.ArtifactPath);
            await InjectAsync(id, [
                new TarFile($"workspace/{artifactName}", artifact.Bytes, File0755),
                new TarFile("workspace/input.txt", Encoding.UTF8.GetBytes(test.Input), File0644),
            ], ct);

            var (status, timedOut) = await StartAndWaitAsync(id, profile.RunTimeout, ct);
            var (stdout, stderr, stdoutTruncated, _) = await ReadLogsAsync(id, OutputCap, StderrCap, ct);
            var state = (await docker.Containers.InspectContainerAsync(id, ct)).State!;

            var actual = stdout;

            var verdict =
                timedOut ? Verdict.TLE :
                state.OOMKilled ? Verdict.MLE :
                stdoutTruncated ? Verdict.OLE :
                status != 0 ? Verdict.RE :
                Tokens(actual) == Tokens(test.Expected) ? Verdict.OK : Verdict.WA;

            return new RunOutcome(verdict, stdout, stderr, status, Elapsed(state));
        }
        finally { await RemoveAsync(id); }
    }

    private async Task InjectAsync(string id, TarFile[] files, CancellationToken ct)
    {
        using var archive = Archives.Create(files);
        await docker.Containers.ExtractArchiveToContainerAsync(
            id, new CopyToContainerParameters { Path = "/" }, archive, ct);
    }

    private async Task<(long Status, bool TimedOut)> StartAndWaitAsync(string id, TimeSpan timeout, CancellationToken ct)
    {
        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");

        await docker.Containers.StartContainerAsync(id, new ContainerStartParameters(), ct);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            var wait = await docker.Containers.WaitContainerAsync(id, cts.Token);
            return (wait.StatusCode, false);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            // Отмена токена рвёт только HTTP-запрос — контейнер надо убить самим
            try
            {
                await docker.Containers.KillContainerAsync(id,
                    new ContainerKillParameters { Signal = "SIGKILL" }, CancellationToken.None);
            }
            catch (DockerContainerNotFoundException) { }
            catch (DockerApiException ex) when (ex.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound)
            {
                // Контейнер мог завершиться между таймаутом и отправкой SIGKILL.
            }

            var wait = await docker.Containers.WaitContainerAsync(id, CancellationToken.None);
            return (wait.StatusCode, true);
        }
    }

    private async Task<(string Stdout, string Stderr, bool StdoutTruncated, bool StderrTruncated)> ReadLogsAsync(
        string id, int stdoutCap, int stderrCap, CancellationToken ct)
    {
        using var logs = await docker.Containers.GetContainerLogsAsync(id,
            new ContainerLogsParameters { ShowStdout = true, ShowStderr = true }, ct);

        using var stdout = new MemoryStream();
        using var stderr = new MemoryStream();
        var buffer = new byte[8192];
        var stdoutTruncated = false;
        var stderrTruncated = false;

        while (true)
        {
            var result = await logs.ReadOutputAsync(buffer, 0, buffer.Length, ct);
            if (result.EOF) break;

            var isStdout = result.Target == MultiplexedStream.TargetStream.StandardOut;
            var target = isStdout ? stdout : stderr;
            var cap = isStdout ? stdoutCap : stderrCap;
            var take = (int)Math.Min(result.Count, Math.Max(0L, cap - target.Length));
            if (take > 0) target.Write(buffer, 0, take);

            if (take < result.Count)
            {
                if (isStdout) stdoutTruncated = true;
                else stderrTruncated = true;
            }
        }

        return (
            Encoding.UTF8.GetString(stdout.ToArray()),
            Encoding.UTF8.GetString(stderr.ToArray()),
            stdoutTruncated,
            stderrTruncated);
    }

    private async Task RemoveAsync(string id)
    {
        try
        {
            await docker.Containers.RemoveContainerAsync(id,
                new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, CancellationToken.None);
        }
        catch (DockerContainerNotFoundException) { }
    }

    private static (string FromImage, string? Tag) SplitImageReference(string image)
    {
        if (string.IsNullOrWhiteSpace(image))
            throw new ArgumentException("Образ контейнера не должен быть пустым.", nameof(image));

        if (image.Contains('@'))
            return (image, null);

        var colon = image.LastIndexOf(':');
        var slash = image.LastIndexOf('/');
        return colon > slash
            ? (image[..colon], image[(colon + 1)..])
            : (image, "latest");
    }

    private static string Tokens(string s) =>
        string.Join(' ', s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

    private static TimeSpan Elapsed(State state)
    {
        if (!TryParseDockerTime(state.StartedAt, out var started) ||
            !TryParseDockerTime(state.FinishedAt, out var finished) ||
            finished <= started)
            return TimeSpan.Zero;

        return finished - started;
    }

    private static bool TryParseDockerTime(string? value, out DateTimeOffset result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = default;
            return false;
        }

        var dot = value.IndexOf('.');
        if (dot >= 0)
        {
            var end = dot + 1;
            while (end < value.Length && char.IsAsciiDigit(value[end])) end++;
            if (end - dot - 1 > 7)
                value = string.Concat(value.AsSpan(0, dot + 8), value.AsSpan(end));
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal, out result);
    }
}