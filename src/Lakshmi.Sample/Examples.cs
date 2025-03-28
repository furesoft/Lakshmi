using System.Runtime.InteropServices;
using Extism;
using Lakshmi.Sample.Models;

namespace Lakshmi.Sample;

[JsonContext(typeof(JsonContext))]
public partial class Examples
{
    [Import("moss", Entry ="addPoint")]
    private static partial int AddPoint();

    [Export("moss_extension_unregister")]
    public static void Unregister()
    {
    }

    [Export("moss_extension_loop")]
    public static void Loop()
    {
    }

    [Export("moss_extension_register")]
    public static ExtensionInfo Register(MossState state)
    {
        Pdk.Log(LogLevel.Error, "working parameter serialisation: height = " + state.Height);

        return new ExtensionInfo();
    }
}