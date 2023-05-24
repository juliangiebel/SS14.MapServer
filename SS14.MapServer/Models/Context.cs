using Microsoft.EntityFrameworkCore;
using SS14.MapServer.Models.Entities;

namespace SS14.MapServer.Models;

public class Context : DbContext
{
    public DbSet<Map>? Maps { get; set; }
    public DbSet<Tile>? Tiles { get; set; }
    public DbSet<ImageFile>? Images { get; set; }

    public Context(DbContextOptions<Context> options) : base(options) {}

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Map>();
        builder.Entity<Grid>();
        builder.Entity<Tile>();
        builder.Entity<ImageFile>();
    }

}