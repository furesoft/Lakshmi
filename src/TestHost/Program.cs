using Extism.Sdk;

namespace TestHost;

class Program
{
    static void Main(string[] args)
    {
        var manifest = new Manifest(new PathWasmSource("LakshmiSample.wasm"));

        //ToDo: add host functions to test return value and calling with parameters
        using var compiledPlugin = new CompiledPlugin(manifest, [

        ], withWasi: true);
        using var plugin = compiledPlugin.Instantiate();

        plugin.AllowHttpResponseHeaders();
        plugin.Call<object>("myFunction", "input");

        //todo: call function with void return type without parameters
        //todo: call function with void return type with parameters
        //todo: call function with return type without parameters
        //todo: call function with return type with parameters
    }
}