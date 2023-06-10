using SS14.MapServer.Models.Types;

namespace SS14.MapServer.Tests;

public class ModelTests
{
    [TestCase(0f, 0f, 0f, 0f,0f, 0f,0f, 0f, 0)]
    [TestCase(0f, 0f, 0f, 0f,0f, 0f,1f, 1f, -1)]
    [TestCase(0f, 0f, 1f, 1f,0f, 0f,0f, 0f, 1)]
    [TestCase(12f, 44f, 23f, 50f,5f, 1533f,2f, 9995f, -1)]
    public void AreaComparisonTest(
        float aAx, float aAy,
        float aBx, float aBy,
        float bAx, float bAy,
        float bBx, float bBy,
        int expected)
    {
        var aA = new Point(aAx, aAy);
        var aB = new Point(aBx, aBy);
        var bA = new Point(bAx, bAy);
        var bB = new Point(bBx, bBy);

        var a = new Area
        {
            A = aA,
            B = aB
        };

        var b = new Area
        {
            A = bA,
            B = bB
        };

        var result = Math.Max(-1, Math.Min(a.CompareTo(b), 1));
        Assert.That(result, Is.EqualTo(expected));
    }
}
