using Present.Data.EntityConfigurations;
using Microsoft.EntityFrameworkCore;

namespace Present.Data;

/// <summary>
///     Represents a session with the <c>giveaway.db</c> database.
/// </summary>
internal sealed class GiveawayContext : DbContext
{
    /// <summary>
    ///     Gets the set of excluded roles.
    /// </summary>
    /// <value>The set of excluded roles.</value>
    public DbSet<ExcludedRole> ExcludedRoles { get; internal set; } = null!;

    /// <summary>
    ///     Gets the set of excluded users.
    /// </summary>
    /// <value>The set of excluded users.</value>
    public DbSet<ExcludedUser> ExcludedUsers { get; internal set; } = null!;

    /// <summary>
    ///     Gets the set of giveaways.
    /// </summary>
    /// <value>The set of giveaways.</value>
    public DbSet<Giveaway> Giveaways { get; internal set; } = null!;

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseSqlite("Data Source=data/giveaway.db");
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new ExcludedRoleConfiguration());
        modelBuilder.ApplyConfiguration(new ExcludedUserConfiguration());
        modelBuilder.ApplyConfiguration(new GiveawayConfiguration());
    }
}
