using Lakshmi.Sample.Models;

namespace Lakshmi.Sample;

[JsonContext(typeof(JsonContext))]
public partial class Examples
{
    [Export("moss_extension_unregister")]
        public static void Unregister(){

        }

    [Export("moss_extension_loop")]
        public static void Loop(){

        }

    [Export("moss_extension_register")]
    public static ExtensionInfo Register()
    {
        return new ExtensionInfo();
    }
}