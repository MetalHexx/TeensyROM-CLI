using System.Text.Json;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Services;
using TeensyRom.Cli.Storage.Entities;

namespace TeensyRom.Cli.Tests.Integration;

[Collection(RealApiCollection.Name)]
public sealed class SettingsAndBanScenarios(SharedApiFixture api)
{
    private const string BanCheckFile = "cli-parity-ban-check.sid";
    private const int MaxDirectoriesWalked = 20;

    [Fact]
    public async Task BanFile_AddsTheNameOnce_AndLeavesEverythingElseAlone()
    {
        var original = await api.Api.GetSettingsAsync();

        try
        {
            using var cli = new CliServices();
            await cli.Get<IStorageService>().BanFile(new SongItem { Name = BanCheckFile, Path = $"/music/{BanCheckFile}" });

            var banned = await api.Api.GetSettingsAsync();

            banned.SearchSettings.BannedFiles.Count(name => name == BanCheckFile).Should().Be(1);
            banned.SearchSettings.BannedFiles.Where(name => name != BanCheckFile)
                .Should().BeEquivalentTo(original.SearchSettings.BannedFiles.Where(name => name != BanCheckFile));
            UnrelatedSections(banned).Should().Be(UnrelatedSections(original));
        }
        finally
        {
            await RestoreAsync(original);
        }

        Json(await api.Api.GetSettingsAsync()).Should().Be(Json(original));
    }

    [SkippableFact]
    public async Task BanFile_HidesTheFileFromItsDirectoryListing()
    {
        Skip.If(api.OnlineDevices.Count == 0, "no TeensyROM is online in the test API");
        var device = api.OnlineDevices[0];
        var storageType = CliServices.AvailableStorageOrSkip(device);

        using var cli = new CliServices();
        cli.SelectDevice(device);
        var storage = cli.Get<IStorageService>();
        storage.SwitchStorage(storageType);

        if (await FindDirectoryHoldingAFile(storage) is not { } found)
        {
            throw new SkipException($"no file within the first {MaxDirectoriesWalked} directories of {device.Name}'s storage");
        }
        var original = await api.Api.GetSettingsAsync();

        try
        {
            await storage.BanFile(found.File);

            var after = await storage.GetDirectory(found.Listing.Path);

            var expected = found.Listing.Files.Select(f => f.Name).Where(name => !name.Equals(found.File.Name, StringComparison.OrdinalIgnoreCase));
            after.Files.Select(f => f.Name).Should().BeEquivalentTo(expected);
        }
        finally
        {
            await RestoreAsync(original);
        }
    }

    /// <summary>
    /// Walks breadth-first from the root for the first directory holding a file the CLI can ban (a launchable one).
    /// </summary>
    private static async Task<FoundFile?> FindDirectoryHoldingAFile(IStorageService storage)
    {
        var pending = new Queue<string>(["/"]);

        for (var walked = 0; walked < MaxDirectoriesWalked && pending.Count > 0; walked++)
        {
            var listing = await storage.GetDirectory(pending.Dequeue());

            if (listing.Files.OfType<ILaunchableItem>().FirstOrDefault() is { } file) return new FoundFile(listing, file);

            foreach (var directory in listing.Directories) pending.Enqueue(directory.Path);
        }
        return null;
    }

    private Task RestoreAsync(GetSettingsResponse original) => api.Api.SaveSettingsAsync(new SaveSettingsRequest
    {
        KnownDevices = original.KnownDevices,
        PlayerSettings = original.PlayerSettings,
        FileTransferSettings = original.FileTransferSettings,
        SearchSettings = original.SearchSettings,
        AppSettings = original.AppSettings
    });

    /// <summary>Everything a ban must not touch: all sections, and in search settings everything but the banned files.</summary>
    private static string UnrelatedSections(GetSettingsResponse settings) => Json(new
    {
        settings.KnownDevices,
        settings.PlayerSettings,
        settings.FileTransferSettings,
        settings.AppSettings,
        settings.SearchSettings.SearchWeights,
        settings.SearchSettings.SearchStopWords,
        settings.SearchSettings.BannedDirectories
    });

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);

    private sealed record FoundFile(DirectoryListing Listing, ILaunchableItem File);
}
