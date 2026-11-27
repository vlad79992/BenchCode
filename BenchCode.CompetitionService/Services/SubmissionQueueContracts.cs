using CompetitionLanguage = BenchCode.CompetitionService.Entities.Language;
using BenchCode.Shared.Entities;

namespace BenchCode.CompetitionService.Services;

public sealed class SubmissionQueueOptions
{
    public string FastQueueName { get; set; } = "benchcode.submissions.fast";
    public string FullQueueName { get; set; } = "benchcode.submissions.full";
    public string DeadLetterExchangeName { get; set; } = "benchcode.submissions.dlx";
    public string DeadLetterQueueName { get; set; } = "benchcode.submissions.dead";
    public ushort PrefetchCount { get; set; } = 1;
    public int FastReplyTimeoutSeconds { get; set; } = 45;
    public int FastMessageTtlMilliseconds { get; set; } = 45_000;
    public int RunnerAttempts { get; set; } = 3;
    public int RunnerRetryDelayMilliseconds { get; set; } = 500;
}

public sealed record SubmissionTestCase(
    Guid TestId,
    int OrderNo,
    string Input,
    string Expected);

public sealed record PublicCheckMessage(
    CompetitionLanguage Language,
    string Code,
    IReadOnlyList<SubmissionTestCase> Tests);

public sealed record FullSubmissionMessage(Guid SubmissionId);

public sealed record PublicCheckRun(
    Guid TestId,
    int TestOrderNo,
    RunOutcome Outcome);

public sealed record PublicCheckResponse(
    bool Completed,
    bool Built,
    string? BuildError,
    Verdict? FinalVerdict,
    IReadOnlyList<PublicCheckRun> Runs,
    string? Error = null);

public interface ISubmissionQueue
{
    Task<PublicCheckResponse> RunPublicCheckAsync(
        PublicCheckMessage message,
        CancellationToken cancellationToken);

    Task EnqueueFullSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken);
}

public interface ISubmissionRunner
{
    Task<SubmissionResult> RunAsync(
        CompetitionLanguage language,
        string code,
        IReadOnlyList<SubmissionTestCase> tests,
        CancellationToken cancellationToken);
}
