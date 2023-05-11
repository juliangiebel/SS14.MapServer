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
}
