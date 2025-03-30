using PolyType;

namespace Lakshmi.Sample.Models;

[GenerateShape]
public readonly partial struct File(string key, string path)
{
    [PropertyShape(Name = "key")]
    public string Key { get; } = key;

    [PropertyShape(Name = "path")]
    public string Path { get; } = path;
}