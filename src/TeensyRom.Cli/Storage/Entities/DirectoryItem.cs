using System.Collections.ObjectModel;
using TeensyRom.Cli.Common;

namespace TeensyRom.Cli.Storage.Entities
{
    public class DirectoryItem : StorageItem 
    {
        public new DirectoryItem Clone() => new DirectoryItem
        {
            IsCompatible = IsCompatible,
            IsFavorite = IsFavorite,
            IsSelected = IsSelected,
            Name = Name,
            Path = Path,
            Size = Size
        };
    }
}