using Newtonsoft.Json;
using TeensyRom.Cli.Settings;
using TeensyRom.Cli.Storage.Entities;
using TeensyFilterType = TeensyRom.Cli.Settings.TeensyFilterType;
using TeensyStorageType = TeensyRom.Cli.Storage.Entities.TeensyStorageType;

namespace TeensyRom.Cli.Tests;

public class SettingsServiceTests
{
    private const string SettingsWithUndefinedMembers = """
        {
          "StorageType": 1,
          "AlwaysPromptStorage": false,
          "WatchDirectoryLocation": "C:\\Users\\someone\\Downloads",
          "TargetRootPath": "/",
          "FileTargets": [
            { "Type": 0, "FilterType": 2, "DisplayName": "SID", "Extension": ".sid" },
            { "Type": 2, "FilterType": 1, "DisplayName": "PRG", "Extension": ".prg" },
            { "Type": 4, "FilterType": 3, "DisplayName": "HEX", "Extension": ".hex" }
          ],
          "FileFilters": [ { "Type": 0, "DisplayName": "All", "Icon": "AllInclusive" } ],
          "AutoTransferPath": "auto-transfer",
          "AutoConnectEnabled": true,
          "StartupFilter": 2,
          "DefaultComPort": "COM3",
          "BannedDirectories": [ "Docs" ],
          "BannedFiles": [ "Super_Trouper.sid" ],
          "SearchWeights": { "Title": 1.0, "FileName": 0.1 },
          "SearchStopWords": [ "the" ],
          "EnableDebugLogs": true,
          "HasIndexed": true
        }
        """;

    [Fact]
    public void Deserialize_FileWithUndefinedMembers_KeepsThePreferencesTheCliOwns()
    {
        var settings = SettingsService.Deserialize(SettingsWithUndefinedMembers);

        settings.Should().NotBeNull();
        settings!.StorageType.Should().Be(TeensyStorageType.USB);
        settings.AlwaysPromptStorage.Should().BeFalse();
        settings.StartupFilter.Should().Be(TeensyFilterType.Music);
        settings.EnableDebugLogs.Should().BeTrue();
        settings.HasIndexed.Should().BeTrue();
        settings.FileTargets.Select(t => t.Type).Should().Equal(TeensyFileType.Sid, TeensyFileType.Prg, TeensyFileType.Hex);
        settings.GetFileTypes(TeensyFilterType.Music).Should().Equal(TeensyFileType.Sid);
        settings.GetFileTypes(TeensyFilterType.All).Should().Equal(TeensyFileType.Sid, TeensyFileType.Prg);
    }

    [Fact]
    public void Deserialize_FileWithUndefinedMembers_DropsThemOnTheNextSave()
    {
        var settings = SettingsService.Deserialize(SettingsWithUndefinedMembers);

        var saved = JsonConvert.SerializeObject(settings, Formatting.Indented);

        saved.Should().NotContainAny("DefaultComPort", "BannedFiles", "BannedDirectories", "WatchDirectoryLocation");
    }
}
