using Microsoft.EntityFrameworkCore;

namespace JobQueueTask.Api.Entities;

public sealed class JobDbContext(DbContextOptions<JobDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(job =>
        {
            job.ToTable("jobs");

            job.HasKey(j => j.Id);

            job.Property(j => j.Type).HasMaxLength(50).IsRequired();

            job.Property(j => j.Result).HasMaxLength(50);

            job.Property(j => j.ErrorMessage).HasMaxLength(256);

            job.Property(j => j.Status).HasConversion<string>();

            // Row version
            job.Property(x => x.RowVersion).IsRowVersion();
        });
    }
}
