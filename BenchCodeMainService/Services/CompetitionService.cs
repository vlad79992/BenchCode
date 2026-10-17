using BenchCodeMainService.Entities;
using BenchCodeMainService.Entities.Dtos;
using Task = BenchCodeMainService.Entities.Task;

namespace BenchCodeMainService.Services;

public class CompetitionService
{
    private readonly List<Group> _groups =
    [
        new Group { Id = 1, Name = "ИВТ-01", StudentIds = [1, 2] },
        new Group { Id = 2, Name = "ИВТ-02", StudentIds = [3, 4] }
    ];

    private readonly List<User> _users =
    [
        new User { Id = 1, Name = "Студент 1", GroupId = 1 },
        new User { Id = 2, Name = "Студент 2", GroupId = 1 },
        new User { Id = 3, Name = "Студент 3", GroupId = 2 },
        new User { Id = 4, Name = "Студент 4", GroupId = 2 }
    ];

    private readonly List<Competition> _competitions;
    private readonly List<Submit> _submissions = [];
    private int _nextSubmissionId = 1;
    private readonly object _submissionLock = new();

    public CompetitionService()
    {
        var now = DateTimeOffset.UtcNow;
        _competitions =
        [
            new Competition
            {
                Id = 1,
                Name = "СиАКОД",
                Description = "Первый семестр",
                StartDate = now.AddDays(-1),
                EndDate = now.AddYears(1),
                GroupIds = [1],
                Tasks =
                [
                    new Task
                    {
                        Id = 101,
                        Name = "Бинарный поиск",
                        Description = "Реализуйте бинарный поиск.",
                        Languages = [Languages.Cpp, Languages.CSharp, Languages.Java, Languages.Python]
                    },
                    new Task
                    {
                        Id = 102,
                        Name = "Сортировка пузырьком",
                        Description = "Реализуйте сортировку пузырьком.",
                        Languages = [Languages.CSharp, Languages.Java]
                    }
                ]
            },
            new Competition
            {
                Id = 2,
                Name = "Оптимизация",
                Description = "Второй семестр",
                StartDate = now.AddDays(-1),
                EndDate = now.AddYears(1),
                GroupIds = [2],
                Tasks =
                [
                    new Task
                    {
                        Id = 201,
                        Name = "Два числа",
                        Description = "Найдите два числа с заданной суммой.",
                        Languages = [Languages.Cpp, Languages.CSharp, Languages.Java, Languages.Python]
                    },
                    new Task
                    {
                        Id = 202,
                        Name = "Ханойская башня",
                        Description = "Реализуйте решение задачи о Ханойской башне.",
                        Languages = [Languages.CSharp, Languages.Java]
                    }
                ]
            }
        ];
    }

    public IReadOnlyList<Competition> GetCompetitions(int? userId = null)
    {
        if (userId is null)
            return _competitions;

        var user = GetUser(userId.Value);
        return user is null
            ? []
            : _competitions.Where(competition => competition.GroupIds.Contains(user.GroupId)).ToList();
    }

    public Competition? GetCompetition(int id) =>
        _competitions.FirstOrDefault(competition => competition.Id == id);

    public IReadOnlyList<Group> GetGroups() => _groups;

    public IReadOnlyList<User> GetUsers() => _users;

    public User? GetUser(int id) => _users.FirstOrDefault(user => user.Id == id);

    public IReadOnlyList<Submit> GetSubmissions(int userId)
    {
        lock (_submissionLock)
            return _submissions.Where(submission => submission.UserId == userId).ToList();
    }

    public SubmissionResult Submit(int taskId, SubmitDto request)
    {
        var user = GetUser(request.UserId);
        if (user is null)
            return SubmissionResult.Failure(StatusCodes.Status404NotFound, "Студент не найден.");

        var competition = _competitions.FirstOrDefault(item => item.Tasks.Any(task => task.Id == taskId));
        if (competition is null)
            return SubmissionResult.Failure(StatusCodes.Status404NotFound, "Задача не найдена.");

        if (!competition.GroupIds.Contains(user.GroupId))
            return SubmissionResult.Failure(StatusCodes.Status403Forbidden, "Задача недоступна вашей группе.");

        var now = DateTimeOffset.UtcNow;
        if (now < competition.StartDate || now > competition.EndDate)
            return SubmissionResult.Failure(StatusCodes.Status409Conflict, "Соревнование сейчас не проводится.");

        var task = competition.Tasks.First(item => item.Id == taskId);
        if (!Enum.IsDefined(request.Language) || !task.Languages.Contains(request.Language))
            return SubmissionResult.Failure(StatusCodes.Status400BadRequest, "Язык не поддерживается этой задачей.");

        if (string.IsNullOrWhiteSpace(request.Code))
            return SubmissionResult.Failure(StatusCodes.Status400BadRequest, "Отправьте непустой код.");

        var accepted = request.Code.Contains("// MOCK_ACCEPTED", StringComparison.Ordinal);
        lock (_submissionLock)
        {
            var submission = new Submit
            {
                Id = _nextSubmissionId++,
                TaskId = taskId,
                UserId = user.Id,
                Code = request.Code,
                Language = request.Language,
                Status = accepted ? SubmitStatus.Accepted : SubmitStatus.Error,
                Message = accepted
                    ? "Заглушка проверки: решение принято."
                    : "Заглушка проверки: для имитации принятого решения добавьте в код комментарий // MOCK_ACCEPTED.",
                SubmittedAt = now
            };

            _submissions.Add(submission);
            task.Submits.Add(submission);
            return SubmissionResult.Success(submission);
        }
    }
}

public sealed record SubmissionResult(int StatusCode, string? Error, Submit? Submission)
{
    public static SubmissionResult Failure(int statusCode, string error) =>
        new(statusCode, error, null);

    public static SubmissionResult Success(Submit submission) =>
        new(StatusCodes.Status201Created, null, submission);
}