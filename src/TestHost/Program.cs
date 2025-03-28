using Extism.Sdk;
using Lakshmi.Sample.Shared;

namespace TestHost;

partial class Program
{
    static void Main(string[] args)
    {
        var manifest = new Manifest(new PathWasmSource("LakshmiSample.wasm"));

        // Host functions hinzufügen
        using var compiledPlugin = new CompiledPlugin(manifest, new HostFunction[]
        {
            HostFunction.FromMethod<int, int, int>("add", null, (plugin, a, b) => a + b),
            HostFunction.FromMethod<int, int>("square", null, (plugin, a) => a * a),
            HostFunction.FromMethod("printHello", null, (plugin) => Console.WriteLine("Hello, World!")),
            HostFunction.FromMethod<int>("printNumber", null, (plugin, a) => Console.WriteLine($"Number: {a}"))
        }, withWasi: true);

        using var plugin = compiledPlugin.Instantiate();

        plugin.AllowHttpResponseHeaders();
        plugin.Call("empty", "");

        var id = plugin.Call<int>("primRet", "");
        var pt = plugin.Call<Point>("initPoint", "");

        plugin.Call("printHello", "");
        plugin.Call("printNumber", "42");

        var result1 = plugin.Call<int>("square", "");
        var result2 = plugin.Call<int>("add", "3,4");
    }
}