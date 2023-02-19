using Present.Data.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Present.Data.EntityConfigurations;

/// <summary>
///     Defines entity configuration for <see cref="Giveaway" />.
/// </summary>
internal sealed class GiveawayConfiguration : IEntityTypeConfiguration<Giveaway>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Giveaway> builder)
    {
        builder.ToTable(nameof(Giveaway));
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id);
        builder.Property(e => e.StartTime).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.EndTime).HasConversion<DateTimeOffsetToBytesConverter>();
        builder.Property(e => e.EndHandled);
        builder.Property(e => e.CreatorId);
        builder.Property(e => e.GuildId);
        builder.Property(e => e.ChannelId);
        builder.Property(e => e.Title);
        builder.Property(e => e.Description);
        builder.Property(e => e.ImageUri).HasConversion<UriToStringConverter>();
        builder.Property(e => e.Entrants).HasConversion<UInt64ListToBytesConverter>();
        builder.Property(e => e.ExcludedRoles).HasConversion<UInt64ListToBytesConverter>();
        builder.Property(e => e.ExcludedUsers).HasConversion<UInt64ListToBytesConverter>();
        builder.Property(e => e.WinnerCount);
        builder.Property(e => e.WinnerIds).HasConversion<UInt64ListToBytesConverter>();
        builder.Property(e => e.MessageId);
    }
}
