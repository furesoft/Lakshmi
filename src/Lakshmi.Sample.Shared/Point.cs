using PolyType;

namespace Lakshmi.Sample.Shared;

[GenerateShape]
public partial class Point(int X, int Y)
{
    public int X { get; init; } = X;

    public int Y { get; init; } = Y;

    public void Deconstruct(out int X, out int Y)
    {
        X = this.X;
        Y = this.Y;
    }
}