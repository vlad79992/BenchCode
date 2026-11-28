using BenchCode.CompetitionService.Entities;
using Microsoft.EntityFrameworkCore;
using CompetitionTask = BenchCode.CompetitionService.Entities.Task;

namespace BenchCode.CompetitionService;

public class CompetitionDbContext(DbContextOptions<CompetitionDbContext> options) : DbContext(options)
{
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<CompetitionVersion> CompetitionVersions => Set<CompetitionVersion>();
    public DbSet<Participation> Participations => Set<Participation>();
    public DbSet<CompetitionTask> Tasks => Set<CompetitionTask>();
    public DbSet<Test> Tests => Set<Test>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionOutboxMessage> SubmissionOutboxMessages => Set<SubmissionOutboxMessage>();
    public DbSet<Run> Runs => Set<Run>();
}
