using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SynTA.Models.Domain;

namespace SynTA.Data.Configurations;

/// <summary>
/// Entity type configuration for the GherkinScenario entity.
/// </summary>
public class GherkinScenarioConfiguration : IEntityTypeConfiguration<GherkinScenario>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<GherkinScenario> builder)
    {
        // Add index for performance
        builder
            .HasIndex(gs => gs.UserStoryId);
    }
}
