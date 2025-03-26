using System.Text.Json.Serialization;
using Lakshmi.Sample.Models;

namespace Lakshmi.Sample;

[JsonSerializable(typeof(ExtensionInfo))]
[JsonSerializable(typeof(File))]
internal partial class JsonContext : JsonSerializerContext
{
}