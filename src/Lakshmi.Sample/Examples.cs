using Extism;
using Lakshmi.Sample.Models;
using Lakshmi.Sample.Shared;

namespace Lakshmi.Sample;

/*

            HostFunction.FromMethod<int, int>("square", null, (plugin, a) => a * a),
            HostFunction.FromMethod("printHello", null, (plugin) => Console.WriteLine("Hello, World!")),
            HostFunction.FromMethod<int>("printNumber", null, (plugin, a) => Console.WriteLine($"Number: {a}"))
 */

public partial class Examples
{
    [Import("extism", Entry ="add")]
    private static extern int Add(int a, int b);

    [Export("empty")]
    public static void Empty()
    {
        Pdk.Log(LogLevel.Debug, Add(1, 2).ToString());
        Pdk.Log(LogLevel.Debug, "Empty called");
    }

    [Export("primRet")]
    public static int primRet()
    {
        return 42;
    }

    [Export("initPoint")]
    public static Point InitPoint()
    {
        Pdk.Log(LogLevel.Debug, "InitPoint called");
        return new Point(1, 2);
    }



    [Export("moss_extension_register")]
    public static ExtensionInfo Register(MossState state)
    {
        Pdk.Log(LogLevel.Error, "working parameter serialisation: height = " + state.Height);

        return new ExtensionInfo();
    }
}