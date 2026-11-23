using BenchCode.CompetitionService.Entities;
using Microsoft.EntityFrameworkCore;

namespace BenchCode.CompetitionService;

public class CompetitionDbContext(DbContextOptions<CompetitionDbContext> options) : DbContext(options)
{
    public DbSet<Entities.Competition> Competitions => Set<Entities.Competition>();
    public DbSet<Participation> Participations => Set<Participation>();
    public DbSet<CompetitionTask> Tasks => Set<CompetitionTask>();
    public DbSet<CompetitionTest> Tests => Set<CompetitionTest>();
    public DbSet<Entities.Submission> Submissions => Set<Entities.Submission>();
    public DbSet<TestResult> TestResults => Set<TestResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Entities.Competition>()
            .HasIndex(competition => competition.Id)
            .IsUnique();

        modelBuilder.Entity<Participation>()
            .HasIndex(participation => new { participation.CompetitionId, participation.UserId })
            .IsUnique();

        modelBuilder.Entity<CompetitionTask>()
            .HasIndex(task => new { task.CompetitionId, task.OrderNo })
            .IsUnique();

        modelBuilder.Entity<CompetitionTest>()
            .HasIndex(test => new { test.TaskId, test.OrderNo });

        modelBuilder.Entity<Submission>()
            .HasIndex(submission => submission.UserId);

        modelBuilder.Entity<Entities.Competition>()
            .HasMany(competition => competition.Participations)
            .WithOne(participation => participation.Competition)
            .HasForeignKey(participation => participation.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Entities.Competition>()
            .HasMany(competition => competition.Tasks)
            .WithOne(task => task.Competition)
            .HasForeignKey(task => task.CompetitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
