using System.Collections.Generic;
using System.Text.Json.Serialization;
using PolyType;

namespace Lakshmi.Sample.Models;

[GenerateShape]
public partial class ExtensionInfo
{
    [JsonPropertyName("files")] public List<File> Files { get; internal set; } = [];
}