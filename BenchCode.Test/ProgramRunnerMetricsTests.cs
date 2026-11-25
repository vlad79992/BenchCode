extern alias RunnerService;
using RunnerEntities = RunnerService::BenchCode.ProgramRunnerService.Entities;

public sealed class ProgramRunnerMetricsTests
{
    [Fact]
    public void Aggregate_UsesMedianAndPreservesIndividualRunTimes()
    {
        var executions = new[]
        {
            CreateRun(TimeSpan.FromMilliseconds(30)),
            CreateRun(TimeSpan.FromMilliseconds(10)),
            CreateRun(TimeSpan.FromMilliseconds(20)),
        };

        var result = RunnerEntities.RunOutcome.Aggregate(executions);

        Assert.Equal(TimeSpan.FromMilliseconds(20), result.Time);
        Assert.Equal(
            [TimeSpan.FromMilliseconds(30), TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20)],
            result.RunTimes);
    }

    [Fact]
    public void Aggregate_AveragesMiddleTimesForEvenRunCount()
    {
        var executions = new[]
        {
            CreateRun(TimeSpan.FromMilliseconds(40)),
            CreateRun(TimeSpan.FromMilliseconds(10)),
            CreateRun(TimeSpan.FromMilliseconds(20)),
            CreateRun(TimeSpan.FromMilliseconds(30)),
        };

        var result = RunnerEntities.RunOutcome.Aggregate(executions);

        Assert.Equal(TimeSpan.FromMilliseconds(25), result.Time);
    }

    [Fact]
    public void Aggregate_UsesNonAcceptedVerdictIfAnyExecutionFails()
    {
        var executions = new[]
        {
            CreateRun(TimeSpan.FromMilliseconds(10)),
            CreateRun(TimeSpan.FromMilliseconds(20), RunnerEntities.Verdict.WA),
            CreateRun(TimeSpan.FromMilliseconds(30)),
        };

        var result = RunnerEntities.RunOutcome.Aggregate(executions);

        Assert.Equal(RunnerEntities.Verdict.WA, result.Verdict);
    }

    private static RunnerEntities.RunOutcome CreateRun(
        TimeSpan time,
        RunnerEntities.Verdict verdict = RunnerEntities.Verdict.AC) =>
        new(verdict, string.Empty, string.Empty, 0, time, [time]);
}
