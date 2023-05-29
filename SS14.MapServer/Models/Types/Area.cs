using Microsoft.EntityFrameworkCore;

namespace SS14.MapServer.Models.Types;

[Owned]
public class Area
{
    public Point A { get; set; } = new();
    public Point B { get; set; } = new();
}