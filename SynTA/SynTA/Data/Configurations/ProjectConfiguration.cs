using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SynTA.Models.Domain;

namespace SynTA.Data.Configurations;

/// <summary>
/// Entity type configuration for the Project entity.
/// </summary>
public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        // Configure Project -> UserStories relationship
        builder
            .HasMany(p => p.UserStories)
            .WithOne(us => us.Project)
            .HasForeignKey(us => us.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add index for performance
        builder
            .HasIndex(p => p.UserId);
    }
}
