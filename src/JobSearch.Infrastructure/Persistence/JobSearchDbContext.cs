using Microsoft.EntityFrameworkCore;
using JobSearch.Domain.Enums;

namespace JobSearch.Infrastructure.Persistence;

public sealed class JobSearchDbContext : DbContext
{
    public DbSet<RecommendationEntity> Recommendations => Set<RecommendationEntity>();

    public JobSearchDbContext(DbContextOptions<JobSearchDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecommendationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CompanyName).IsRequired();
            entity.Property(e => e.JobTitle).IsRequired();
            entity.Property(e => e.JobUrl).IsRequired();
            entity.Property(e => e.AgeGroup).HasConversion<string>();
        });
    }
}

public sealed class RecommendationEntity
{
    public int Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public string JobUrl { get; set; } = string.Empty;
    public AgeGroup AgeGroup { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string ExperienceLevel { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int SalaryMillionKrw { get; set; }
    public string Summary { get; set; } = string.Empty;
    public double SuitabilityScore { get; set; }
}
