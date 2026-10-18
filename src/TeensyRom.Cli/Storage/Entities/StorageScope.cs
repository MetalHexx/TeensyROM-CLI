using System.ComponentModel;
using System.Net.NetworkInformation;

namespace TeensyRom.Cli.Storage.Entities
{
    public enum StorageScope
    {
        [Description("Selected Storage")]
        Storage,
        [Description("Directory (Deep)")]
        DirDeep,
        [Description("Directory (Shallow)")]
        DirShallow
    }
}