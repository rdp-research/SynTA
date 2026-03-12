using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SynTA.Models.Domain;

namespace SynTA.Data.Configurations;

/// <summary>
/// Entity type configuration for the CypressScript entity.
/// </summary>
public class CypressScriptConfiguration : IEntityTypeConfiguration<CypressScript>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CypressScript> builder)
    {
        // Add index for performance
        builder
            .HasIndex(cs => cs.UserStoryId);
    }
}
