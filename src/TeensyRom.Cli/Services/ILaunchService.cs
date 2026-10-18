using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Settings;

namespace TeensyRom.Cli.Services;

/// <summary>
/// Launches files on the selected TeensyROM through tr-web.
/// </summary>
internal interface ILaunchService
{
    /// <summary>Launches the file; <see cref="LaunchOutcome.Item"/> is <paramref name="item"/> itself.</summary>
    /// <exception cref="Api.TeensyApiException">tr-web didn't answer, or no device is selected.</exception>
    Task<LaunchOutcome> LaunchFile(TeensyStorageType storage, ILaunchableItem item);

    /// <summary>Has tr-web pick a random file within the scope and launch it; <see cref="LaunchOutcome.Item"/> is the file it picked.</summary>
    /// <exception cref="Api.TeensyApiException">tr-web didn't answer, or no device is selected.</exception>
    Task<LaunchOutcome> LaunchRandom(TeensyStorageType storage, StorageScope scope, string startingDirectory, TeensyFilterType filter);
}

internal enum LaunchStatus
{
    Launched,
    /// <summary>tr-web answered but the TeensyROM can't run the file.</summary>
    Incompatible,
    /// <summary>The device, storage or file doesn't exist, or a random pick found no files.</summary>
    NotFound,
    /// <summary>tr-web refused or failed the launch.</summary>
    Failed
}

/// <summary>
/// How a launch ended. <paramref name="Item"/> is set for <see cref="LaunchStatus.Launched"/> and <see cref="LaunchStatus.Incompatible"/>;
/// <paramref name="Message"/> is tr-web's own wording where it gave any.
/// </summary>
internal sealed record LaunchOutcome(LaunchStatus Status, ILaunchableItem? Item, string? Message)
{
    /// <summary>
    /// What tr-web answers when a random launch has no file to pick. Matched by exact text, so re-check it against
    /// tr-web's LaunchRandom endpoint whenever <see cref="Api.ApiPin.Version"/> moves.
    /// </summary>
    public const string NoFilesFoundMessage = "No files were found.";
}
