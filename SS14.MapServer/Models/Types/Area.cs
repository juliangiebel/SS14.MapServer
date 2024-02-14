using System.Numerics;
using Microsoft.EntityFrameworkCore;

namespace SS14.MapServer.Models.Types;

[Owned]
public class Area : IComparable<Area>
{
    public Point A { get; set; } = new();
    public Point B { get; set; } = new();

    public int CompareTo(Area? other)
    {
        if (other == null)
            return -1;


        var sizeA = B - A;
        var sizeB = other.B - other.A;

        var area = Math.Abs(sizeA.X * sizeA.Y);
        var areaB = Math.Abs(sizeB.X * sizeB.Y);

        return area.CompareTo(areaB);
    }

    public Point GetSize()
    {
        return A - B;
    }

    public int GetArea()
    {
        var size = GetSize();
        return (int)Math.Abs(size.X * size.Y);
    }
}
