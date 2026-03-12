using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SynTA.Models.Domain;

namespace SynTA.Data.Configurations;

/// <summary>
/// Entity type configuration for the UserStory entity.
/// </summary>
public class UserStoryConfiguration : IEntityTypeConfiguration<UserStory>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserStory> builder)
    {
        // Configure UserStory -> GherkinScenarios relationship
        builder
            .HasMany(us => us.GherkinScenarios)
            .WithOne(gs => gs.UserStory)
            .HasForeignKey(gs => gs.UserStoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure UserStory -> CypressScripts relationship
        builder
            .HasMany(us => us.CypressScripts)
            .WithOne(cs => cs.UserStory)
            .HasForeignKey(cs => cs.UserStoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add index for performance
        builder
            .HasIndex(us => us.ProjectId);
    }
}
