namespace Lakshmi.Sample.Models;

using System.Text.Json.Serialization;
using System.Collections.Generic;

public class MossState
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("current_screen")]
    public string CurrentScreen { get; set; }

    [JsonPropertyName("opened_context_menus")]
    public List<string> OpenedContextMenus { get; set; }

    [JsonPropertyName("icons")]
    public string[] Icons { get; set; }
}