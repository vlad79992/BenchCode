using BenchCodeMainService.Entities;
using Task = BenchCodeMainService.Entities.Task;

namespace BenchCodeMainService.Services;

public class CompetitionService
{
    public List<Competition> Competitions { get; init; }
    public List<Submission> Submissions { get; init; } = [];
    private readonly object submissionLock = new();

    public CompetitionService()
    {
        var now = DateTimeOffset.UtcNow;
        Competitions =
        [
            new Competition
            {
                Id = Guid.NewGuid(),
                Name = "СиАКОД",
                Description = "Первый семестр",
                StartDate = now.AddDays(-1),
                EndDate = now.AddYears(1),
                Tasks =
                [
                    new Task
                    {
                        Id = Guid.NewGuid(),
                        Name = "Бинарный поиск",
                        Description = "Реализуйте бинарный поиск.",
                        Languages = [Language.Cpp, Language.CSharp, Language.Java, Language.Python]
                    },
                    new Task
                    {
                        Id = Guid.NewGuid(),
                        Name = "Сортировка пузырьком",
                        Description = "Реализуйте сортировку пузырьком.",
                        Languages = [Language.CSharp, Language.Java]
                    }
                ],
                Password = "password"
            },
            new Competition
            {
                Id = Guid.NewGuid(),
                Name = "Оптимизация",
                Description = "Второй семестр",
                StartDate = now.AddDays(-1),
                EndDate = now.AddYears(1),
                Tasks =
                [
                    new Task
                    {
                        Id = Guid.NewGuid(),
                        Name = "Два числа",
                        Description = "Найдите два числа с заданной суммой.",
                        Languages = [Language.Cpp, Language.CSharp, Language.Java, Language.Python]
                    },
                    new Task
                    {
                        Id = Guid.NewGuid(),
                        Name = "Ханойская башня",
                        Description = "Реализуйте решение задачи о Ханойской башне.",
                        Languages = [Language.CSharp, Language.Java]
                    }
                ],
                Password = "password"
            }
        ];
    }

    public Competition? GetCompetition(Guid id) =>
        Competitions.FirstOrDefault(competition => competition.Id == id);

    public IReadOnlyList<Submission> GetSubmissions(Guid taskId)
    {
        lock (submissionLock)
        {
            return Submissions.Where(submission => submission.TaskId == taskId).ToList();
        }
    }

    public IResult Submit(Guid taskId, Submission submission)
    {
        if (Competitions.FirstOrDefault(item => item.Tasks.Any(task => task.Id == taskId)) is not Competition competition)
            return Results.NotFound("Задача не найдена.");

        var now = DateTimeOffset.UtcNow;
        if (now < competition.StartDate || now > competition.EndDate)
            return Results.Conflict("Соревнование сейчас не проводится.");

        var task = competition.Tasks.First(item => item.Id == taskId);
        if (!Enum.IsDefined(submission.Language) || !task.Languages.Contains(submission.Language))
            return Results.BadRequest("Язык не поддерживается этой задачей.");

        var accepted = submission.Code.Contains("// MOCK_ACCEPTED", StringComparison.Ordinal);
        lock (submissionLock)
        {
            Submissions.Add(submission);
            task.Submissions.Add(submission);
            return Results.Ok(submission);
        }
    }
}