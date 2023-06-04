using Microsoft.EntityFrameworkCore;

namespace SS14.MapServer.Models.Types;

[Owned]
public class Area : IComparable<Area>
{
    public Point A { get; set; } = new();
    public Point B { get; set; } = new();


    public int CompareTo(object? obj)
    {
        throw new NotImplementedException();
    }

    public int CompareTo(Area? other)
    {
        if (other == null)
            return -1;

        var area = A.Length * B.Length;
        var areaB = other.A.Length * other.B.Length;

        return area.CompareTo(areaB);
    }
}
