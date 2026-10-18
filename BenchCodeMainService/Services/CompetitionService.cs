using BenchCodeMainService.Entities;
using Task = BenchCodeMainService.Entities.Task;

namespace BenchCodeMainService.Services;

public class CompetitionService
{
    public List<Competition> Competitions { get; init; }
    public List<Submission> Submissions { get; init; } = [];
    public List<Task> Tasks { get; init; }
    private readonly object submissionLock = new();

    public CompetitionService()
    {
        var now = DateTimeOffset.UtcNow;
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
            },
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
        ];
        Competitions =
        [
            new Competition
            {
                Id = Guid.NewGuid(),
                Name = "СиАКОД",
                Description = "Первый семестр",
                StartDate = now.AddDays(-1),
                EndDate = now.AddYears(1),
                TasksIds =
                [
                    Tasks[0].Id, Tasks[1].Id
                ],
                Password = "password_hash"
            },
            new Competition
            {
                Id = Guid.NewGuid(),
                Name = "Оптимизация",
                Description = "Второй семестр",
                StartDate = now.AddDays(-1),
                EndDate = now.AddYears(1),
                TasksIds =
                [
                    Tasks[2].Id, Tasks[3].Id
                ],
                Password = "password_hash"
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
        if (Competitions.FirstOrDefault(c => c.TasksIds.Contains(taskId)) is not Competition competition)
            return Results.NotFound("Задача не найдена.");

        var now = DateTimeOffset.UtcNow;
        if (now < competition.StartDate || now > competition.EndDate)
            return Results.Conflict("Соревнование сейчас не проводится.");

        var task = Tasks.FirstOrDefault(t => t.Id == taskId)!;
        if (!Enum.IsDefined(submission.Language) || !task.Languages.Contains(submission.Language))
            return Results.BadRequest("Язык не поддерживается этой задачей.");

        var accepted = submission.Code.Contains("// MOCK_ACCEPTED", StringComparison.Ordinal);
        lock (submissionLock)
        {
            Submissions.Add(submission);
            return Results.Ok(submission);
        }
    }
}