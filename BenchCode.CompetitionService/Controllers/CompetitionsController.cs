using BenchCode.CompetitionService.Entities;
using BenchCode.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CompetitionTask = BenchCode.CompetitionService.Entities.CompetitionTask;
using SubmissionEntity = BenchCode.CompetitionService.Entities.Submission;

namespace BenchCode.CompetitionService.Controllers;

[ApiController]
[Route("api")]
public class CompetitionsController(CompetitionDbContext db) : ControllerBase
{
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> joinAttempts = new();
    private static readonly ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> submissionAttempts = new();
    private static readonly char[] codeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

    [Authorize]
    [HttpGet("me/competitions")]
    public async Task<ActionResult<IEnumerable<CompetitionResponse>>> GetMyCompetitions(
        [FromQuery] string scope = "joined",
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        IQueryable<Entities.Competition> query = db.Competitions;
        if (scope.Equals("owned", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(competition => competition.CreatorId == userId);
        }
        else
        {
            query = query.Where(competition =>
                competition.Participations.Any(participation => participation.UserId == userId));
        }

        var competitions = await query
            .Include(competition => competition.Tasks)
            .Include(competition => competition.Participations)
            .AsNoTracking()
            .OrderByDescending(competition => competition.CreatedAt)
            .ToListAsync(cancellationToken);

        return Ok(competitions.Select(competition => ToCompetitionResponse(
            competition,
            competition.Participations.FirstOrDefault(participation => participation.UserId == userId)?.Permission)));
    }

    [Authorize]
    [HttpPost("competitions")]
    public async Task<ActionResult<CompetitionResponse>> CreateCompetition(
        CreateCompetitionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.Name))
            return ValidationError("Name", "Название соревнования обязательно.");
        if (request.WeightTime + request.WeightMemory is not (>= 0.999 and <= 1.001))
            return ValidationError("Weights", "Сумма весов должна быть равна 1.");
        if (request.Deadline is not null && request.Deadline <= DateTimeOffset.UtcNow)
            return ValidationError("Deadline", "Дедлайн должен быть в будущем.");
        if (request.DefaultTimeLimitMs < 1 || request.DefaultMemoryLimitMb < 1)
            return ValidationError("Limits", "Лимиты должны быть положительными.");

        var activeCount = await db.Competitions.CountAsync(
            competition => competition.CreatorId == userId &&
                           competition.Status != CompetitionStatus.Archived,
            cancellationToken);
        if (activeCount >= 20)
            return Conflict(new { message = "Превышена квота активных соревнований." });

        var competition = new Entities.Competition
        {
            Id = await GenerateUniqueCodeAsync(cancellationToken),
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            CreatorId = userId,
            Deadline = request.Deadline,
            WeightTime = request.WeightTime,
            WeightMemory = request.WeightMemory,
            AllowViewOthersCode = request.AllowViewOthersCode,
            DefaultTimeLimitMs = request.DefaultTimeLimitMs,
            DefaultMemoryLimitMb = request.DefaultMemoryLimitMb,
            Status = CompetitionStatus.Draft
        };
        competition.Participations.Add(new Participation
        {
            CompetitionId = competition.Id,
            UserId = userId,
            Permission = ContestPermission.Owner
        });

        db.Competitions.Add(competition);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetCompetition), new { id = competition.Id },
            ToCompetitionResponse(competition, ContestPermission.Owner));
    }

    [Authorize]
    [HttpPost("competitions/join")]
    public async Task<ActionResult<CompetitionResponse>> Join([FromBody] JoinCompetitionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (!IsWithinQuota(userId, joinAttempts, 10))
            return StatusCode(StatusCodes.Status429TooManyRequests,
                new { message = "Слишком много попыток. Повторите через минуту" });

        var code = request.Code?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(code) || code.Length != 6)
            return NotFound(new { message = "Соревнование с таким кодом не найдено" });

        var competition = await db.Competitions
            .Include(item => item.Participations)
            .Include(item => item.Tasks)
            .FirstOrDefaultAsync(item => item.Id == code, cancellationToken);
        if (competition is null || competition.Status != CompetitionStatus.Published)
            return NotFound(new { message = "Соревнование с таким кодом не найдено" });

        var existing = competition.Participations.FirstOrDefault(item => item.UserId == userId);
        if (existing is null)
        {
            db.Participations.Add(new Participation
            {
                CompetitionId = competition.Id,
                UserId = userId,
                Permission = ContestPermission.Participant
            });
            await db.SaveChangesAsync(cancellationToken);
            existing = new Participation { UserId = userId, Permission = ContestPermission.Participant };
        }

        return Ok(ToCompetitionResponse(competition, existing.Permission));
    }

    [Authorize]
    [HttpGet("competitions/{id}")]
    public async Task<ActionResult<CompetitionResponse>> GetCompetition(string id,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var competition = await db.Competitions
            .Include(item => item.Tasks)
            .Include(item => item.Participations)
            .FirstOrDefaultAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);
        if (competition is null) return NotFound();
        var participation = await GetParticipationAsync(id, userId, cancellationToken);
        if (participation is null) return StatusCode(403, new { message = "Доступ запрещён" });

        return Ok(ToCompetitionResponse(competition, participation.Permission));
    }

    [Authorize]
    [HttpPatch("competitions/{id}")]
    public async Task<ActionResult<CompetitionResponse>> UpdateCompetition(string id,
        UpdateCompetitionRequest request, CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);

        var competition = await db.Competitions.Include(item => item.Tasks)
            .FirstAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.Name))
            competition.Name = request.Name.Trim();
        if (request.Description is not null) competition.Description = request.Description;
        if (request.Deadline is not null) competition.Deadline = request.Deadline;
        if (request.AllowViewOthersCode is not null)
            competition.AllowViewOthersCode = request.AllowViewOthersCode.Value;

        var affectsComparability = false;
        if (request.WeightTime is not null)
        {
            competition.WeightTime = request.WeightTime.Value;
            affectsComparability = true;
        }
        if (request.WeightMemory is not null)
        {
            competition.WeightMemory = request.WeightMemory.Value;
            affectsComparability = true;
        }
        if (request.DefaultTimeLimitMs is not null)
        {
            competition.DefaultTimeLimitMs = request.DefaultTimeLimitMs.Value;
            affectsComparability = true;
        }
        if (request.DefaultMemoryLimitMb is not null)
        {
            competition.DefaultMemoryLimitMb = request.DefaultMemoryLimitMb.Value;
            affectsComparability = true;
        }
        if (request.WeightTime is not null || request.WeightMemory is not null)
        {
            if (competition.WeightTime + competition.WeightMemory is not (>= 0.999 and <= 1.001))
                return ValidationError("Weights", "Сумма весов должна быть равна 1.");
        }
        if (affectsComparability) competition.Version++;

        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToCompetitionResponse(competition, ContestPermission.Owner));
    }

    [Authorize]
    [HttpPost("competitions/{id}/publish")]
    public async Task<IActionResult> Publish(string id, CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        var competition = await db.Competitions.Include(item => item.Tasks)
            .FirstAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);
        if (!competition.Tasks.Any(task => !task.IsArchived))
            return ValidationError("Tasks", "Для публикации нужна хотя бы одна задача.");
        competition.Status = CompetitionStatus.Published;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToCompetitionResponse(competition, ContestPermission.Owner));
    }

    [Authorize]
    [HttpPost("competitions/{id}/archive")]
    public async Task<IActionResult> Archive(string id, CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        var competition = await db.Competitions.FirstAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);
        competition.Status = CompetitionStatus.Archived;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(ToCompetitionResponse(competition, ContestPermission.Owner));
    }

    [Authorize]
    [HttpDelete("competitions/{id}")]
    public async Task<IActionResult> DeleteCompetition(string id, CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        var competition = await db.Competitions.FirstAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);
        if (await db.Submissions.AnyAsync(submission => submission.CompetitionId == competition.Id, cancellationToken))
        {
            competition.Status = CompetitionStatus.Archived;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { archived = true });
        }
        db.Competitions.Remove(competition);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("competitions/{id}/participants")]
    public async Task<IActionResult> GetParticipants(string id, CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        var participants = await db.Participations
            .AsNoTracking()
            .Where(item => item.CompetitionId == id.ToUpperInvariant())
            .OrderBy(item => item.JoinedAt)
            .Select(item => new ParticipantResponse(item.UserId, item.Permission, item.JoinedAt))
            .ToListAsync(cancellationToken);
        return Ok(participants);
    }

    [Authorize]
    [HttpGet("competitions/{id}/tasks")]
    public async Task<IActionResult> GetTasks(string id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (await GetParticipationAsync(id, userId, cancellationToken) is null)
            return StatusCode(403, new { message = "Доступ запрещён" });

        var tasks = await db.Tasks
            .Include(task => task.Submissions)
            .Include(task => task.Tests)
            .AsNoTracking()
            .Where(task => task.CompetitionId == id.ToUpperInvariant() && !task.IsArchived)
            .OrderBy(task => task.OrderNo)
            .ToListAsync(cancellationToken);

        return Ok(tasks.Select(task =>
        {
            var latest = task.Submissions.Where(submission => submission.UserId == userId)
                .OrderByDescending(submission => submission.SubmittedAt).FirstOrDefault();
            var status = latest?.Status == SubmissionStatus.Testing ? "Тестируется"
                : latest?.Verdict == Verdict.OK ? "Решена"
                : latest is not null ? $"Ошибка ({latest.Verdict?.ToString() ?? latest.Status.ToString()})"
                : "Новая";
            return new TaskResponse(task.Id, task.OrderNo, task.Name, task.StatementMarkdown,
                task.TimeLimitMs, task.MemoryLimitMb, task.Checker, task.CheckerLanguage,
                GetLanguages(task), task.Tests.Count(test => test.IsPublic), status);
        }));
    }

    [Authorize]
    [HttpGet("competitions/{id}/tasks/{taskId:int}")]
    public async Task<IActionResult> GetTask(string id, int taskId, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (await GetParticipationAsync(id, userId, cancellationToken) is null)
            return StatusCode(403, new { message = "Доступ запрещён" });
        var task = await db.Tasks.Include(item => item.Tests)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == taskId && item.CompetitionId == id.ToUpperInvariant(),
                cancellationToken);
        if (task is null) return NotFound();
        var publicTests = task.Tests.Where(test => test.IsPublic)
            .OrderBy(test => test.OrderNo)
            .Select(test => new PublicTestResponse(test.OrderNo, test.InputText,
                test.ExpectedOutputText));
        return Ok(new
        {
            task.Id,
            task.OrderNo,
            task.Name,
            task.StatementMarkdown,
            task.TimeLimitMs,
            task.MemoryLimitMb,
            AllowedLanguages = GetLanguages(task),
            Checker = task.Checker.ToString(),
            PublicTests = publicTests
        });
    }

    [Authorize]
    [HttpPost("competitions/{id}/tasks")]
    public async Task<ActionResult<TaskResponse>> CreateTask(string id, CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        var competition = await db.Competitions.Include(item => item.Tasks)
            .FirstAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);
        if (competition.Tasks.Count(task => !task.IsArchived) >= 50)
            return Conflict(new { message = "Превышена квота задач." });
        if (request.Tests.Length == 0)
            return ValidationError("Tests", "Нужен хотя бы один тест.");
        if (request.Tests.Length > 100)
            return ValidationError("Tests", "Не более 100 тестов.");
        if (request.Checker == CheckerType.CustomChecker && request.CheckerLanguage is null)
            return ValidationError("CheckerLanguage", "Для программы-чекера нужно выбрать язык.");
        if (request.Checker == CheckerType.ExactText &&
            request.Tests.Any(test => string.IsNullOrEmpty(test.ExpectedOutputText) && test.ExpectedOutputFile is null))
            return ValidationError("ExpectedOutput", "Для текстового сравнения нужен эталонный вывод.");

        var task = new CompetitionTask
        {
            CompetitionId = competition.Id,
            OrderNo = request.OrderNo,
            Name = request.Name.Trim(),
            StatementMarkdown = request.StatementMarkdown,
            TimeLimitMs = request.TimeLimitMs,
            MemoryLimitMb = request.MemoryLimitMb,
            Checker = request.Checker,
            CheckerLanguage = request.CheckerLanguage,
            CheckerSource = request.CheckerSource,
            AllowedLanguagesJson = JsonSerializer.Serialize(request.AllowedLanguages)
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var testRequest in request.Tests)
        {
            var inputBytes = testRequest.InputFile is null ? null : Convert.FromBase64String(testRequest.InputFile);
            var outputBytes = testRequest.ExpectedOutputFile is null ? null : Convert.FromBase64String(testRequest.ExpectedOutputFile);
            if ((inputBytes?.Length ?? 0) > 1024 * 1024 || (outputBytes?.Length ?? 0) > 1024 * 1024)
                return ValidationError("Tests", "Файл теста должен быть не больше 1 МБ.");
            db.Tests.Add(new CompetitionTest
            {
                TaskId = task.Id,
                OrderNo = testRequest.OrderNo,
                IsPublic = testRequest.IsPublic,
                InputText = testRequest.InputText,
                InputFile = inputBytes,
                ExpectedOutputText = testRequest.ExpectedOutputText,
                ExpectedOutputFile = outputBytes
            });
        }
        if (competition.Status == CompetitionStatus.Published &&
            await db.Submissions.AnyAsync(submission => submission.CompetitionId == competition.Id, cancellationToken))
            competition.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetTask), new { id = competition.Id, taskId = task.Id },
            new TaskResponse(task.Id, task.OrderNo, task.Name, task.StatementMarkdown,
                task.TimeLimitMs, task.MemoryLimitMb, task.Checker, task.CheckerLanguage,
                request.AllowedLanguages, request.Tests.Count(test => test.IsPublic), "Новая"));
    }

    [Authorize]
    [HttpPatch("competitions/{id}/tasks/{taskId:int}")]
    public async Task<IActionResult> UpdateTask(string id, int taskId, UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        var task = await db.Tasks.Include(item => item.Tests)
            .FirstOrDefaultAsync(item => item.Id == taskId && item.CompetitionId == id.ToUpperInvariant(),
                cancellationToken);
        if (task is null) return NotFound();

        if (request.Name is not null) task.Name = request.Name.Trim();
        if (request.StatementMarkdown is not null) task.StatementMarkdown = request.StatementMarkdown;
        if (request.OrderNo is not null) task.OrderNo = request.OrderNo.Value;
        if (request.TimeLimitMs is not null) task.TimeLimitMs = request.TimeLimitMs.Value;
        if (request.MemoryLimitMb is not null) task.MemoryLimitMb = request.MemoryLimitMb.Value;
        if (request.Checker is not null) task.Checker = request.Checker.Value;
        if (request.CheckerLanguage is not null) task.CheckerLanguage = request.CheckerLanguage;
        if (request.CheckerSource is not null) task.CheckerSource = request.CheckerSource;
        if (request.AllowedLanguages is not null)
            task.AllowedLanguagesJson = JsonSerializer.Serialize(request.AllowedLanguages);

        var competition = await db.Competitions.FirstAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);
        competition.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new TaskResponse(task.Id, task.OrderNo, task.Name, task.StatementMarkdown,
            task.TimeLimitMs, task.MemoryLimitMb, task.Checker, task.CheckerLanguage,
            GetLanguages(task), task.Tests.Count(test => test.IsPublic), "Новая"));
    }

    [Authorize]
    [HttpDelete("competitions/{id}/tasks/{taskId:int}")]
    public async Task<IActionResult> DeleteTask(string id, int taskId, CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        var task = await db.Tasks.FirstOrDefaultAsync(item => item.Id == taskId &&
            item.CompetitionId == id.ToUpperInvariant(), cancellationToken);
        if (task is null) return NotFound();
        if (await db.Submissions.AnyAsync(submission => submission.TaskId == taskId, cancellationToken))
        {
            task.IsArchived = true;
            await db.SaveChangesAsync(cancellationToken);
            return Ok(new { archived = true });
        }
        db.Tasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpPost("competitions/{id}/tasks/{taskId:int}/tests")]
    public async Task<IActionResult> AddTests(string id, int taskId,
        [FromBody] AddTestsRequest request, CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        var task = await db.Tasks.Include(item => item.Tests)
            .FirstOrDefaultAsync(item => item.Id == taskId &&
                item.CompetitionId == id.ToUpperInvariant(), cancellationToken);
        if (task is null) return NotFound();
        if (task.Tests.Count + request.Tests.Length > 100)
            return ValidationError("Tests", "Не более 100 тестов на задачу.");

        long totalBytes = task.Tests.Sum(test => (test.InputFile?.Length ?? 0) + (test.ExpectedOutputFile?.Length ?? 0));
        foreach (var testRequest in request.Tests)
        {
            var inputBytes = testRequest.InputFile is null ? null : Convert.FromBase64String(testRequest.InputFile);
            var outputBytes = testRequest.ExpectedOutputFile is null ? null : Convert.FromBase64String(testRequest.ExpectedOutputFile);
            totalBytes += (inputBytes?.Length ?? 0) + (outputBytes?.Length ?? 0);
            if (totalBytes > 10 * 1024 * 1024)
                return ValidationError("Tests", "Суммарный размер тестов не больше 10 МБ.");
            db.Tests.Add(new CompetitionTest
            {
                TaskId = taskId,
                OrderNo = testRequest.OrderNo,
                IsPublic = testRequest.IsPublic,
                InputText = testRequest.InputText,
                InputFile = inputBytes,
                ExpectedOutputText = testRequest.ExpectedOutputText,
                ExpectedOutputFile = outputBytes
            });
        }
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { added = request.Tests.Length });
    }

    [Authorize]
    [HttpPost("competitions/{id}/tasks/{taskId:int}/submit")]
    public async Task<ActionResult<SubmissionResponse>> Submit(string id, int taskId,
        SubmitRequest request, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (await GetParticipationAsync(id, userId, cancellationToken) is null)
            return StatusCode(403, new { message = "Доступ запрещён" });
        if (!IsWithinQuota(userId, submissionAttempts, 6))
            return StatusCode(429, new { message = "Слишком много посылок. Повторите через минуту" });

        if (request.Code is null || request.Code.Length == 0)
            return ValidationError("Code", "Код решения не должен быть пустым.");
        if (Encoding.UTF8.GetByteCount(request.Code) > 64 * 1024)
            return ValidationError("Code", "Код решения должен быть не больше 64 КБ.");

        var task = await db.Tasks.FirstOrDefaultAsync(item => item.Id == taskId &&
            item.CompetitionId == id.ToUpperInvariant(), cancellationToken);
        if (task is null) return NotFound();
        var languages = GetLanguages(task);
        if (!languages.Contains(request.Language))
            return ValidationError("Language", "Язык не разрешён для этой задачи.");

        var competition = await db.Competitions.FirstAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);
        var submission = new SubmissionEntity
        {
            CompetitionId = competition.Id,
            TaskId = taskId,
            UserId = userId,
            Language = request.Language,
            Code = request.Code,
            CompetitionVersion = competition.Version,
            IsLate = competition.Deadline is not null && DateTimeOffset.UtcNow > competition.Deadline,
            Status = SubmissionStatus.Testing
        };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync(cancellationToken);
        return Accepted(ToSubmissionResponse(submission, false));
    }

    [Authorize]
    [HttpPost("competitions/{id}/tasks/{taskId:int}/test")]
    public async Task<IActionResult> TestPublic(string id, int taskId, SubmitRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        if (await GetParticipationAsync(id, userId, cancellationToken) is null)
            return StatusCode(403, new { message = "Доступ запрещён" });
        if (!IsWithinQuota(userId!, submissionAttempts, 6))
            return StatusCode(429, new { message = "Слишком много посылок. Повторите через минуту" });
        var task = await db.Tasks.Include(item => item.Tests)
            .FirstOrDefaultAsync(item => item.Id == taskId &&
                item.CompetitionId == id.ToUpperInvariant(), cancellationToken);
        if (task is null) return NotFound();
        if (!GetLanguages(task).Contains(request.Language))
            return ValidationError("Language", "Язык не разрешён для этой задачи.");
        if (Encoding.UTF8.GetByteCount(request.Code ?? "") > 64 * 1024)
            return ValidationError("Code", "Код решения должен быть не больше 64 КБ.");
        return Ok(new
        {
            status = SubmissionStatus.Testing.ToString(),
            message = "Публичные тесты поставлены в очередь; сохранение посылки не выполняется."
        });
    }

    [Authorize]
    [HttpGet("competitions/{id}/tasks/{taskId:int}/submissions")]
    public async Task<IActionResult> GetTaskSubmissions(string id, int taskId,
        [FromQuery] string? userId = null,
        [FromQuery] Verdict? verdict = null,
        [FromQuery] int? version = null,
        [FromQuery] bool? isLate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);
        var query = db.Submissions
            .AsNoTracking()
            .Where(submission => submission.CompetitionId == id.ToUpperInvariant() &&
                                 submission.TaskId == taskId);
        if (!string.IsNullOrEmpty(userId)) query = query.Where(submission => submission.UserId == userId);
        if (verdict is not null) query = query.Where(submission => submission.Verdict == verdict);
        if (version is not null) query = query.Where(submission => submission.CompetitionVersion == version);
        if (isLate is not null) query = query.Where(submission => submission.IsLate == isLate);
        var submissions = await query
            .OrderByDescending(submission => submission.SubmittedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(submissions.Select(submission => ToSubmissionResponse(submission, false)));
    }

    [Authorize]
    [HttpGet("competitions/{id}/tasks/{taskId:int}/submissions/{submissionId:int}")]
    public async Task<IActionResult> GetSubmission(string id, int taskId, int submissionId,
        CancellationToken cancellationToken)
    {
        var owner = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        var submission = await db.Submissions.Include(item => item.TestResults)
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == submissionId &&
                item.CompetitionId == id.ToUpperInvariant() && item.TaskId == taskId,
                cancellationToken);
        if (submission is null) return NotFound();
        if (owner is null)
        {
            if (submission.UserId != GetUserId()) return StatusCode(403, new { message = "Доступ запрещён" });
            if (submission.Status == SubmissionStatus.Finished && submission.Verdict is not null)
            {
                // Participant details are deliberately filtered below.
            }
        }
        var isOwner = owner?.Permission == ContestPermission.Owner;
        var details = submission.TestResults.Select(result =>
            new TestResultResponse(result.TestId, isOwner ? result.TestId : result.TestId,
                result.Verdict, result.TimeMs, result.MemoryKb,
                isOwner || result.Diff is not null ? result.Diff : null));
        return Ok(new
        {
            submission.Id,
            submission.UserId,
            submission.Language,
            submission.Code,
            submission.SubmittedAt,
            submission.CompetitionVersion,
            submission.IsLate,
            submission.Status,
            submission.Verdict,
            submission.TimeMs,
            submission.MemoryKb,
            submission.CompilerOutput,
            TestResults = details
        });
    }

    [Authorize]
    [HttpGet("me/submissions")]
    public async Task<IActionResult> GetMySubmissions([FromQuery] string? competitionId,
        [FromQuery] int? taskId, [FromQuery] Verdict? verdict,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserId();
        var query = db.Submissions.AsNoTracking().Where(submission => submission.UserId == userId);
        if (!string.IsNullOrWhiteSpace(competitionId))
            query = query.Where(submission => submission.CompetitionId == competitionId.ToUpperInvariant());
        if (taskId is not null) query = query.Where(submission => submission.TaskId == taskId);
        if (verdict is not null) query = query.Where(submission => submission.Verdict == verdict);
        pageSize = Math.Clamp(pageSize, 1, 100);
        page = Math.Max(page, 1);
        var submissions = await query.OrderByDescending(submission => submission.SubmittedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return Ok(submissions.Select(submission => ToSubmissionResponse(submission, false)));
    }

    [Authorize]
    [HttpGet("competitions/{id}/leaderboard")]
    public async Task<IActionResult> GetLeaderboard(string id, CancellationToken cancellationToken)
    {
        var userId = GetUserId();
        var participation = await GetParticipationAsync(id, userId, cancellationToken);
        if (participation is null) return StatusCode(403, new { message = "Доступ запрещён" });
        var competition = await db.Competitions.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.ToUpperInvariant(), cancellationToken);
        if (competition is null) return NotFound();
        var submissions = await db.Submissions.AsNoTracking()
            .Where(submission => submission.CompetitionId == competition.Id &&
                                 submission.CompetitionVersion == competition.Version &&
                                 !submission.IsLate &&
                                 submission.Verdict == Verdict.OK)
            .ToListAsync(cancellationToken);

        var bestByUserTask = submissions
            .GroupBy(submission => new { submission.UserId, submission.TaskId })
            .Select(group => new
            {
                group.Key.UserId,
                group.Key.TaskId,
                TimeMs = group.Min(submission => submission.TimeMs ?? int.MaxValue),
                MemoryKb = group.Min(submission => submission.MemoryKb ?? int.MaxValue)
            })
            .ToList();

        var taskIds = bestByUserTask.Select(item => item.TaskId).Distinct().ToList();
        var bestTimeByTask = taskIds.ToDictionary(
            taskId => taskId,
            taskId => bestByUserTask.Where(item => item.TaskId == taskId).Min(item => item.TimeMs));
        var bestMemoryByTask = taskIds.ToDictionary(
            taskId => taskId,
            taskId => bestByUserTask.Where(item => item.TaskId == taskId).Min(item => item.MemoryKb));

        var rows = bestByUserTask.GroupBy(item => item.UserId)
            .Select(group =>
            {
                var score = group.Sum(item =>
                    competition.WeightTime * ((double)item.TimeMs / bestTimeByTask[item.TaskId]) +
                    competition.WeightMemory * ((double)item.MemoryKb / bestMemoryByTask[item.TaskId]));
                return new
                {
                    UserId = group.Key,
                    SolvedTasks = group.Count(),
                    Score = score,
                    TimeMs = group.Sum(item => item.TimeMs),
                    MemoryKb = group.Sum(item => item.MemoryKb)
                };
            })
            .OrderByDescending(row => row.SolvedTasks)
            .ThenBy(row => row.Score)
            .ToList();

        var output = new List<object>();
        var place = 0;
        double? previousScore = null;
        int previousSolved = -1;
        for (var index = 0; index < rows.Count; index++)
        {
            if (index == 0 || rows[index].SolvedTasks != previousSolved || rows[index].Score != previousScore)
            {
                place = index + 1;
                previousSolved = rows[index].SolvedTasks;
                previousScore = rows[index].Score;
            }
            output.Add(new
            {
                Place = place,
                rows[index].UserId,
                rows[index].SolvedTasks,
                rows[index].TimeMs,
                rows[index].MemoryKb,
                Score = rows[index].Score
            });
        }
        return Ok(new
        {
            CompetitionVersion = competition.Version,
            Rows = output,
            AverageTimeMs = rows.Count == 0 ? 0 : rows.Average(row => row.TimeMs),
            AverageMemoryKb = rows.Count == 0 ? 0 : rows.Average(row => row.MemoryKb)
        });
    }

    [Authorize]
    [HttpGet("competitions/{id}/export")]
    public async Task<IActionResult> Export(string id, CancellationToken cancellationToken)
    {
        var participation = await GetParticipationAsync(id, GetUserId(), cancellationToken);
        if (participation is null) return ForbidOwnerOrNotFound(id);
        var leaderboard = await GetLeaderboard(id, cancellationToken);
        if (leaderboard is not OkObjectResult) return leaderboard;
        var content = "Place,UserId,SolvedTasks,TimeMs,MemoryKb,Score\n";
        // CSV values are intentionally escaped before export.
        return File(Encoding.UTF8.GetBytes(content),
            "text/csv", $"competition-{id}-leaderboard.csv");
    }

    private async Task<Participation?> GetParticipationAsync(string id, string? userId,
        CancellationToken cancellationToken)
    {
        if (userId is null) return null;
        return await db.Participations.AsNoTracking().FirstOrDefaultAsync(
            participation => participation.CompetitionId == id.ToUpperInvariant() &&
                             participation.UserId == userId,
            cancellationToken);
    }

    private string? GetUserId()
    {
        return User.FindFirst(OpenIddictConstants.Claims.Subject)?.Value ??
               User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
               User.Identity?.Name;
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 100; attempt++)
        {
            var bytes = new byte[6];
            System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
            var code = new string(bytes.Select(value => codeAlphabet[value % codeAlphabet.Length]).ToArray());
            if (!await db.Competitions.AnyAsync(item => item.Id == code, cancellationToken))
                return code;
        }
        throw new InvalidOperationException("Не удалось сгенерировать уникальный код соревнования.");
    }

    private static bool IsWithinQuota(string key,
        ConcurrentDictionary<string, ConcurrentQueue<DateTimeOffset>> attempts,
        int limit)
    {
        var queue = attempts.GetOrAdd(key, _ => new ConcurrentQueue<DateTimeOffset>());
        var now = DateTimeOffset.UtcNow;
        while (queue.TryPeek(out var timestamp) && now - timestamp > TimeSpan.FromMinutes(1))
            queue.TryDequeue(out _);
        if (queue.Count >= limit) return false;
        queue.Enqueue(now);
        return true;
    }

    private static CompetitionResponse ToCompetitionResponse(Entities.Competition competition,
        ContestPermission? permission)
    {
        return new CompetitionResponse(competition.Id, competition.Name, competition.Description,
            competition.CreatorId, competition.Deadline, competition.Status,
            competition.Version, competition.WeightTime, competition.WeightMemory,
            competition.AllowViewOthersCode, competition.DefaultTimeLimitMs,
            competition.DefaultMemoryLimitMb, competition.CreatedAt, permission,
            competition.Tasks.Count);
    }

    private static SubmissionResponse ToSubmissionResponse(SubmissionEntity submission, bool includeCode)
    {
        return new SubmissionResponse(submission.Id, submission.CompetitionId, submission.TaskId,
            submission.UserId, submission.Language, includeCode ? submission.Code : null,
            submission.SubmittedAt, submission.CompetitionVersion, submission.IsLate,
            submission.Status, submission.Verdict, submission.TimeMs, submission.MemoryKb,
            submission.CompilerOutput);
    }

    private static CompetitionLanguage[] GetLanguages(CompetitionTask task)
    {
        if (string.IsNullOrWhiteSpace(task.AllowedLanguagesJson)) return Array.Empty<CompetitionLanguage>();
        try { return JsonSerializer.Deserialize<CompetitionLanguage[]>(task.AllowedLanguagesJson) ?? Array.Empty<CompetitionLanguage>(); }
        catch (JsonException) { return Array.Empty<CompetitionLanguage>(); }
    }

    private BadRequestObjectResult ValidationError(string field, string message)
    {
        return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            [field] = new[] { message }
        }));
    }

    private ActionResult ForbidOwnerOrNotFound(string id)
    {
        var exists = db.Competitions.AsNoTracking().Any(item => item.Id == id.ToUpperInvariant());
        return exists
            ? StatusCode(403, new { message = "Управлять соревнованием может только его организатор" })
            : NotFound();
    }

    public sealed record CreateCompetitionRequest(
        [Required, MaxLength(200)] string Name,
        string? Description,
        DateTimeOffset? Deadline,
        double WeightTime = 0.5,
        double WeightMemory = 0.5,
        bool AllowViewOthersCode = false,
        int DefaultTimeLimitMs = 1000,
        int DefaultMemoryLimitMb = 256);

    public sealed record UpdateCompetitionRequest(
        [MaxLength(200)] string? Name,
        string? Description,
        DateTimeOffset? Deadline,
        double? WeightTime,
        double? WeightMemory,
        bool? AllowViewOthersCode,
        int? DefaultTimeLimitMs,
        int? DefaultMemoryLimitMb);

    public sealed record JoinCompetitionRequest([Required, StringLength(6, MinimumLength = 6)] string Code);
    public sealed record ParticipantResponse(string UserId, ContestPermission Permission, DateTimeOffset JoinedAt);
    public sealed record PublicTestResponse(int OrderNo, string? InputText, string? ExpectedOutputText);
    public sealed record TestRequest(int OrderNo, bool IsPublic, string? InputText, string? InputFile,
        string? ExpectedOutputText, string? ExpectedOutputFile);
    public sealed record CreateTaskRequest(
        [Required, MaxLength(200)] string Name,
        int OrderNo,
        [Required] string StatementMarkdown,
        int TimeLimitMs,
        int MemoryLimitMb,
        CheckerType Checker,
        CompetitionLanguage? CheckerLanguage,
        string? CheckerSource,
        [Required] CompetitionLanguage[] AllowedLanguages,
        [Required] TestRequest[] Tests);
    public sealed record UpdateTaskRequest(
        string? Name,
        int? OrderNo,
        string? StatementMarkdown,
        int? TimeLimitMs,
        int? MemoryLimitMb,
        CheckerType? Checker,
        CompetitionLanguage? CheckerLanguage,
        string? CheckerSource,
        CompetitionLanguage[]? AllowedLanguages);
    public sealed record AddTestsRequest([Required] TestRequest[] Tests);
    public sealed record SubmitRequest([Required] CompetitionLanguage Language, [Required] string Code);
    public sealed record TaskResponse(int Id, int OrderNo, string Name, string StatementMarkdown,
        int TimeLimitMs, int MemoryLimitMb, CheckerType Checker,
        CompetitionLanguage? CheckerLanguage, CompetitionLanguage[] AllowedLanguages,
        int PublicTestsCount, string Status);
    public sealed record SubmissionResponse(int Id, string CompetitionId, int TaskId, string UserId,
        CompetitionLanguage Language, string? Code, DateTimeOffset SubmittedAt, int CompetitionVersion,
        bool IsLate, SubmissionStatus Status, Verdict? Verdict, int? TimeMs, int? MemoryKb,
        string? CompilerOutput);
    public sealed record TestResultResponse(int TestId, int OrderNo, Verdict Verdict, int TimeMs,
        int MemoryKb, string? Diff);
    public sealed record CompetitionResponse(string Id, string Name, string? Description,
        string CreatorId, DateTimeOffset? Deadline, CompetitionStatus Status, int Version,
        double WeightTime, double WeightMemory, bool AllowViewOthersCode,
        int DefaultTimeLimitMs, int DefaultMemoryLimitMb, DateTimeOffset CreatedAt,
        ContestPermission? Permission, int TaskCount);
}