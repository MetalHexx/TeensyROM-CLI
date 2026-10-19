using TeensyRom.Cli.Settings;
using TeensyRom.Cli.Storage.Entities;

namespace TeensyRom.Cli.Services;

/// <summary>
/// The selected TeensyROM's storage as tr-web serves it: directories, search, indexing, favorites and bans.
/// </summary>
/// <remarks>Every failure arrives as a <see cref="Api.TeensyApiException"/> carrying tr-web's own wording.</remarks>
internal interface IStorageService
{
    /// <summary>The storage every call works against.</summary>
    TeensyStorageType StorageType { get; }

    /// <summary>Works against another storage from now on and saves it as the CLI's default-storage preference.</summary>
    void SwitchStorage(TeensyStorageType storageType);

    /// <summary>Lists one directory of the current storage.</summary>
    Task<DirectoryListing> GetDirectory(string path);

    /// <summary>Searches the current storage only; results that are not launchable are dropped.</summary>
    Task<IReadOnlyList<ILaunchableItem>> Search(string query, TeensyFilterType filter);

    /// <summary>Indexes a directory and everything below it; an empty path or "/" indexes the whole storage.</summary>
    Task Index(string path);

    /// <summary>Tags the file as a favorite and flips <see cref="IStorageItem.IsFavorite"/> on success.</summary>
    Task SaveFavorite(ILaunchableItem item);

    /// <summary>Untags the favorite and clears <see cref="IStorageItem.IsFavorite"/> on success.</summary>
    Task RemoveFavorite(ILaunchableItem item);

    /// <summary>Adds the file's name to tr-web's banned files. Needs no selected device.</summary>
    Task BanFile(ILaunchableItem item);
}

/// <summary>
/// The directories and files directly inside one storage directory.
/// </summary>
internal sealed class DirectoryListing
{
    public string Path { get; set; } = string.Empty;
    public List<DirectoryItem> Directories { get; set; } = [];
    public List<IFileItem> Files { get; set; } = [];
}
