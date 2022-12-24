using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Present.Data.EntityConfigurations;

/// <summary>
///     Defines entity configuration for <see cref="ExcludedUser" />.
/// </summary>
internal sealed class ExcludedUserConfiguration : IEntityTypeConfiguration<ExcludedUser>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExcludedUser> builder)
    {
        builder.ToTable(nameof(ExcludedUser));
        builder.HasKey(e => new {e.GuildId, e.UserId});

        builder.Property(e => e.GuildId);
        builder.Property(e => e.UserId);
        builder.Property(e => e.StaffMemberId);
        builder.Property(e => e.Reason);
    }
}
