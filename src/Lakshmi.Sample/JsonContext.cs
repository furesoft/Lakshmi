using System.Text.Json.Serialization;
using Lakshmi.Sample.Models;
using System;

namespace Lakshmi.Sample;

[JsonSerializable(typeof(ExtensionInfo))]
[JsonSerializable(typeof(File))]
[JsonSerializable(typeof(MossState))]
[JsonSerializable(typeof(ExamplesRegisterParameters))]
internal partial class JsonContext : JsonSerializerContext
{
}