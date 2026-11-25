extern alias RunnerService;

using RunnerEntities = RunnerService::BenchCode.ProgramRunnerService.Entities;

namespace BenchCode.Test;

public sealed class ProgramRunnerMetricsTests
{
    [Fact]
    public void Aggregate_UsesMiddleSampleForOddRunCount()
    {
        var result = RunnerEntities.RunOutcome.Aggregate(
        [
            Outcome(TimeSpan.FromMilliseconds(90)),
            Outcome(TimeSpan.FromMilliseconds(10)),
            Outcome(TimeSpan.FromMilliseconds(20)),
        ]);

        Assert.Equal(TimeSpan.FromMilliseconds(20), result.Time);
        Assert.Equal(
            new[]
            {
                TimeSpan.FromMilliseconds(90),
                TimeSpan.FromMilliseconds(10),
                TimeSpan.FromMilliseconds(20),
            },
            result.RunTimes);
    }

    [Fact]
    public void Aggregate_AveragesMiddleSamplesForEvenRunCount()
    {
        var result = RunnerEntities.RunOutcome.Aggregate(
        [
            Outcome(TimeSpan.FromMilliseconds(90)),
            Outcome(TimeSpan.FromMilliseconds(10)),
            Outcome(TimeSpan.FromMilliseconds(50)),
            Outcome(TimeSpan.FromMilliseconds(30)),
        ]);

        Assert.Equal(TimeSpan.FromMilliseconds(40), result.Time);
    }

    [Fact]
    public void Aggregate_PreservesNonAcceptedVerdict()
    {
        var result = RunnerEntities.RunOutcome.Aggregate(
        [
            Outcome(TimeSpan.FromMilliseconds(10)),
            Outcome(TimeSpan.FromMilliseconds(20), RunnerEntities.Verdict.WA),
            Outcome(TimeSpan.FromMilliseconds(30)),
        ]);

        Assert.Equal(RunnerEntities.Verdict.WA, result.Verdict);
        Assert.Equal(TimeSpan.FromMilliseconds(20), result.Time);
    }

    [Fact]
    public void Aggregate_RejectsEmptyRunList()
    {
        Assert.Throws<ArgumentException>(() => RunnerEntities.RunOutcome.Aggregate([]));
    }

    [Fact]
    public void SubmissionResult_FinalUsesBuildAndRunVerdicts()
    {
        var buildFailure = new RunnerEntities.SubmissionResult(false, "compile failed", []);
        var accepted = new RunnerEntities.SubmissionResult(true, null, []);
        var wrongAnswer = new RunnerEntities.SubmissionResult(
            true,
            null,
            [Outcome(TimeSpan.FromMilliseconds(10), RunnerEntities.Verdict.WA)]);

        Assert.Equal(RunnerEntities.Verdict.CE, buildFailure.Final);
        Assert.Equal(RunnerEntities.Verdict.AC, accepted.Final);
        Assert.Equal(RunnerEntities.Verdict.WA, wrongAnswer.Final);
    }

    private static RunnerEntities.RunOutcome Outcome(
        TimeSpan time,
        RunnerEntities.Verdict verdict = RunnerEntities.Verdict.AC) =>
        new(verdict, string.Empty, string.Empty, 0, time);
}
