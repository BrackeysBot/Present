using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Present.Data.EntityConfigurations;

/// <summary>
///     Defines entity configuration for <see cref="T:Present.Data.ExcludedRole" />.
/// </summary>
internal sealed class ExcludedRoleConfiguration : IEntityTypeConfiguration<ExcludedRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ExcludedRole> builder)
    {
        builder.ToTable(nameof(ExcludedRole));
        builder.HasKey(e => new {e.GuildId, e.RoleId});

        builder.Property(e => e.GuildId);
        builder.Property(e => e.RoleId);
        builder.Property(e => e.StaffMemberId);
        builder.Property(e => e.Reason);
    }
}
