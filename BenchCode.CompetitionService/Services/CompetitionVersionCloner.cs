using BenchCode.CompetitionService.Entities;
using CompetitionTask = BenchCode.CompetitionService.Entities.Task;
using CompetitionTest = BenchCode.CompetitionService.Entities.Test;

namespace BenchCode.CompetitionService.Services;

public static class CompetitionVersionCloner
{
    public static CompetitionVersionClone Clone(
        CompetitionVersion source,
        int versionNumber,
        Guid? excludedTaskId = null,
        Guid? excludedTestId = null)
    {
        var version = new CompetitionVersion
        {
            CompetitionId = source.CompetitionId,
            VersionNumber = versionNumber,
            Name = source.Name,
            Description = source.Description,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            AllowViewOthersCode = source.AllowViewOthersCode
        };
        var taskCopies = new Dictionary<Guid, CompetitionTask>();
        var testCopies = new Dictionary<Guid, CompetitionTest>();

        foreach (var oldTask in source.Tasks.OrderBy(task => task.OrderNo))
        {
            if (excludedTaskId == oldTask.Id)
                continue;

            var newTask = new CompetitionTask
            {
                CompetitionVersion = version,
                OrderNo = oldTask.OrderNo,
                Name = oldTask.Name,
                Description = oldTask.Description,
                TimeLimitMs = oldTask.TimeLimitMs,
                MemoryLimitMb = oldTask.MemoryLimitMb,
                IsArchived = oldTask.IsArchived
            };

            foreach (var oldTest in oldTask.Tests.OrderBy(test => test.OrderNo))
            {
                if (excludedTestId == oldTest.Id)
                    continue;

                var newTest = new CompetitionTest
                {
                    Task = newTask,
                    OrderNo = oldTest.OrderNo,
                    IsPublic = oldTest.IsPublic,
                    Input = oldTest.Input,
                    Expected = oldTest.Expected
                };
                newTask.Tests.Add(newTest);
                testCopies.Add(oldTest.Id, newTest);
            }

            version.Tasks.Add(newTask);
            taskCopies.Add(oldTask.Id, newTask);
        }

        return new CompetitionVersionClone(version, taskCopies, testCopies);
    }
}

public sealed record CompetitionVersionClone(
    CompetitionVersion Version,
    IReadOnlyDictionary<Guid, CompetitionTask> TaskCopies,
    IReadOnlyDictionary<Guid, CompetitionTest> TestCopies);
