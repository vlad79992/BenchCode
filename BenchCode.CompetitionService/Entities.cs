using System.ComponentModel.DataAnnotations;

namespace BenchCode.CompetitionService.Entities;

public enum CompetitionStatus
{
    Draft,
    Published,
    Archived
}

public enum ContestPermission
{
    Owner,
    Participant
}

public enum CheckerType
{
    ExactText,
    CustomChecker
}

public enum SubmissionStatus
{
    Testing,
    Finished,
    SystemError
}

public enum Verdict
{
    AC,
    WA,
    CE,
    RE,
    TLE,
    MLE
}

public class Competition
{
    [Key]
    [MaxLength(6)]
    public string Id { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Required]
    public string CreatorId { get; set; } = string.Empty;
    public DateTimeOffset? Deadline { get; set; }
    public CompetitionStatus Status { get; set; } = CompetitionStatus.Draft;
    public int Version { get; set; } = 1;
    public double WeightTime { get; set; } = 0.5;
    public double WeightMemory { get; set; } = 0.5;
    public bool AllowViewOthersCode { get; set; }
    public int DefaultTimeLimitMs { get; set; } = 1000;
    public int DefaultMemoryLimitMb { get; set; } = 256;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Participation> Participations { get; set; } = new List<Participation>();
    public ICollection<CompetitionTask> Tasks { get; set; } = new List<CompetitionTask>();
}

public class Participation
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string CompetitionId { get; set; } = string.Empty;
    [Required]
    public string UserId { get; set; } = string.Empty;
    public ContestPermission Permission { get; set; } = ContestPermission.Participant;
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
    public Competition? Competition { get; set; }
}

public class CompetitionTask
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string CompetitionId { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    public string StatementMarkdown { get; set; } = string.Empty;
    public int TimeLimitMs { get; set; } = 1000;
    public int MemoryLimitMb { get; set; } = 256;
    public CheckerType Checker { get; set; } = CheckerType.ExactText;
    public CompetitionLanguage? CheckerLanguage { get; set; }
    public string? CheckerSource { get; set; }
    public string AllowedLanguagesJson { get; set; } = "[]";
    public bool IsArchived { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Competition? Competition { get; set; }
    public ICollection<CompetitionTest> Tests { get; set; } = new List<CompetitionTest>();
    public ICollection<Submission> Submissions { get; set; } = new List<Submission>();
}

public class CompetitionTest
{
    [Key]
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int OrderNo { get; set; }
    public bool IsPublic { get; set; }
    public string? InputText { get; set; }
    public byte[]? InputFile { get; set; }
    public string? ExpectedOutputText { get; set; }
    public byte[]? ExpectedOutputFile { get; set; }
    public CompetitionTask? Task { get; set; }
}

public class Submission
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string CompetitionId { get; set; } = string.Empty;
    public int TaskId { get; set; }
    [Required]
    public string UserId { get; set; } = string.Empty;
    public CompetitionLanguage Language { get; set; }
    [Required, MaxLength(65536)]
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
    public int CompetitionVersion { get; set; } = 1;
    public bool IsLate { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Testing;
    public Verdict? Verdict { get; set; }
    public int? TimeMs { get; set; }
    public int? MemoryKb { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CompilerOutput { get; set; }

    public CompetitionTask? Task { get; set; }
    public ICollection<TestResult> TestResults { get; set; } = new List<TestResult>();
}

public class TestResult
{
    [Key]
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int TestId { get; set; }
    public Verdict Verdict { get; set; }
    public int TimeMs { get; set; }
    public int MemoryKb { get; set; }
    public string? Diff { get; set; }
    public Submission? Submission { get; set; }
    public CompetitionTest? Test { get; set; }
}

public enum CompetitionLanguage
{
    Cpp,
    CSharp,
    Java,
    C,
    Python
}