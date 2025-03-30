using PolyType;

namespace Lakshmi.Sample.Models;

using System.Collections.Generic;

[GenerateShape]
public partial class MossState
{
    [PropertyShape(Name = "width")]
    public int Width { get; set; }

    [PropertyShape(Name = "height")]
    public int Height { get; set; }

    [PropertyShape(Name = "current_screen")]
    public string CurrentScreen { get; set; }

    [PropertyShape(Name = "opened_context_menus")]
    public List<string> OpenedContextMenus { get; set; }

    [PropertyShape(Name = "icons")]
    public string[] Icons { get; set; }
}