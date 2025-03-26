using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Lakshmi.Sample.Models;

public class ExtensionInfo
{
    [JsonPropertyName("files")] public List<File> Files { get; internal set; } = [];
}