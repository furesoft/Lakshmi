using System.Text.Json.Serialization;

namespace Lakshmi.Sample.Models;

public readonly struct File(string key, string path)
{
    [JsonPropertyName("key")] public string Key { get; } = key;

    [JsonPropertyName("path")] public string Path { get; } = path;
}