using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace SS14.MapServer.Models.Types;

[Owned]
public class Point
{
    public float X {get; set;}
    public float Y {get; set;}

    public Point()
    {
    }

    public Point(float x, float y)
    {
        X = x;
        Y = y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Point operator -(Point left, Point right)
    {
        return new Point(
            left.X - right.X,
            left.Y - right.Y
        );
    }
}
