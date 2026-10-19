using System.Globalization;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Services;
using TeensyRom.Cli.Storage.Entities;
using ApiStorageType = TeensyRom.Cli.Api.Generated.TeensyStorageType;
using CliFilterType = TeensyRom.Cli.Settings.TeensyFilterType;
using CliStorageType = TeensyRom.Cli.Storage.Entities.TeensyStorageType;

namespace TeensyRom.Cli.Api;

/// <summary>
/// Turns tr-web's generated DTOs into the CLI's own models, and the CLI's enums into the generated ones.
/// </summary>
internal static class ApiMapper
{
    public static ApiStorageType ToApi(this CliStorageType storage) => storage switch
    {
        CliStorageType.SD => ApiStorageType.SD,
        CliStorageType.USB => ApiStorageType.USB,
        _ => throw new ArgumentOutOfRangeException(nameof(storage), storage, null)
    };

    public static NullableOfTeensyFilterType ToApi(this CliFilterType filter) => filter switch
    {
        CliFilterType.All => NullableOfTeensyFilterType.All,
        CliFilterType.Games => NullableOfTeensyFilterType.Games,
        CliFilterType.Music => NullableOfTeensyFilterType.Music,
        CliFilterType.Hex => NullableOfTeensyFilterType.Hex,
        CliFilterType.Images => NullableOfTeensyFilterType.Images,
        _ => throw new ArgumentOutOfRangeException(nameof(filter), filter, null)
    };

    public static Scope ToApi(this StorageScope scope) => scope switch
    {
        StorageScope.Storage => Scope.Storage,
        StorageScope.DirDeep => Scope.DirDeep,
        StorageScope.DirShallow => Scope.DirShallow,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, null)
    };

    public static DirectoryListing ToListing(StorageCacheDto dto) => new()
    {
        Path = dto.Path,
        Directories = dto.Directories.Select(d => new DirectoryItem { Name = d.Name, Path = d.Path }).ToList(),
        Files = dto.Files.Select(ToFileItem).ToList()
    };

    /// <summary>
    /// Maps a file to its item type; anything tr-web doesn't classify becomes a plain, non-launchable <see cref="FileItem"/>.
    /// </summary>
    public static IFileItem ToFileItem(FileItemDto dto)
    {
        FileItem item = dto.Type switch
        {
            FileItemType.Song => ToSong(dto),
            FileItemType.Game => new GameItem(),
            FileItemType.Image => new ImageItem(),
            FileItemType.Hex => new HexItem(),
            _ => new FileItem()
        };

        item.Name = dto.Name;
        item.Path = dto.Path;
        item.Size = dto.Size;
        item.IsFavorite = dto.IsFavorite;
        item.IsCompatible = dto.IsCompatible;
        item.Title = dto.Title;
        item.Creator = dto.Creator;
        item.ReleaseInfo = dto.ReleaseInfo;
        item.Description = dto.Description;
        item.ShareUrl = dto.ShareUrl;
        item.MetadataSource = dto.MetadataSource;
        item.Meta1 = dto.Meta1;
        item.Meta2 = dto.Meta2;
        item.MetadataSourcePath = dto.MetadataSourcePath;
        return item;
    }

    public static ILaunchableItem? ToLaunchable(FileItemDto dto) => ToFileItem(dto) as ILaunchableItem;

    private static SongItem ToSong(FileItemDto dto)
    {
        var song = new SongItem { StartSubtuneNum = dto.StartSubtuneNum };

        if (TryParseLength(dto.PlayLength, out var playLength)) song.PlayLength = playLength;

        foreach (var subtune in dto.SubtuneLengths)
        {
            if (TryParseLength(subtune, out var length)) song.SubtuneLengths.Add(length);
        }
        return song;
    }

    private static bool TryParseLength(string? text, out TimeSpan length)
    {
        length = default;
        return !string.IsNullOrWhiteSpace(text) && TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out length);
    }
}
