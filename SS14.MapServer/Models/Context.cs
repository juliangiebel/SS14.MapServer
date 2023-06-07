using Microsoft.EntityFrameworkCore;
using SS14.MapServer.Models.Entities;

namespace SS14.MapServer.Models;

public class Context : DbContext
{
    public DbSet<Map>? Map { get; set; }
    public DbSet<Tile>? Tile { get; set; }
    public DbSet<ImageFile>? Image { get; set; }

    public DbSet<PullRequestComment>? PullRequestComment { get; set; }

    public Context(DbContextOptions<Context> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder builder)
    {
        //[Index(nameof(GitRef), nameof(MapId))]
        builder.Entity<Map>()
            .HasIndex(map => new { map.GitRef, map.MapId})
            .IsUnique();

        builder.Entity<Map>()
            .Property(e => e.LastUpdated)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<Grid>();
        builder.Entity<Tile>();

        builder.Entity<ImageFile>()
            .Property(e => e.LastUpdated)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Entity<PullRequestComment>();
    }

}
