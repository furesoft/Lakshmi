using PolyType;

namespace Lakshmi.Sample.Shared;

[GenerateShape]
public partial record Rect(Point TopLeft, Point BottomRight);