using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SynTA.Models.Domain;

namespace SynTA.Data.Configurations;

/// <summary>
/// Entity type configuration for the UserSettings entity.
/// </summary>
public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        // Configure ApplicationUser -> UserSettings relationship (one-to-one)
        builder
            .HasOne(us => us.User)
            .WithOne()
            .HasForeignKey<UserSettings>(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add unique index on UserSettings.UserId to enforce one settings per user
        builder
            .HasIndex(us => us.UserId)
            .IsUnique();
    }
}
