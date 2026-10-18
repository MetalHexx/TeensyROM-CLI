using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Settings;
using TeensyRom.Cli.Storage.Entities;
using TeensyFilterType = TeensyRom.Cli.Settings.TeensyFilterType;
using TeensyStorageType = TeensyRom.Cli.Storage.Entities.TeensyStorageType;

namespace TeensyRom.Cli.Services;

internal sealed class LaunchService(ITeensyRomApiClient api, IDeviceService devices) : ILaunchService
{
    public async Task<LaunchOutcome> LaunchFile(TeensyStorageType storage, ILaunchableItem item)
    {
        var deviceId = devices.RequireDeviceId();

        try
        {
            var response = await ApiErrors.Call(() => api.LaunchFileAsync(deviceId, storage.ToApi(), item.Path));

            return response.IsCompatible
                ? new LaunchOutcome(LaunchStatus.Launched, item, response.Message)
                : new LaunchOutcome(LaunchStatus.Incompatible, item, response.Message);
        }
        catch (TeensyApiException ex) when (ex.StatusCode is not null)
        {
            return Refused(ex);
        }
    }

    public async Task<LaunchOutcome> LaunchRandom(TeensyStorageType storage, StorageScope scope, string startingDirectory, TeensyFilterType filter)
    {
        var deviceId = devices.RequireDeviceId();

        try
        {
            var response = await ApiErrors.Call(() => api.LaunchRandomAsync(deviceId, storage.ToApi(), filter.ToApi(), scope.ToApi(), startingDirectory));

            if (ApiMapper.ToLaunchable(response.LaunchedFile) is not { } item)
            {
                return new LaunchOutcome(LaunchStatus.Failed, null, response.Message);
            }
            item.IsCompatible = response.IsCompatible;

            return response.IsCompatible
                ? new LaunchOutcome(LaunchStatus.Launched, item, response.Message)
                : new LaunchOutcome(LaunchStatus.Incompatible, item, response.Message);
        }
        catch (TeensyApiException ex) when (ex.StatusCode is not null)
        {
            return Refused(ex);
        }
    }

    private static LaunchOutcome Refused(TeensyApiException ex) =>
        new(ex.StatusCode == 404 ? LaunchStatus.NotFound : LaunchStatus.Failed, null, ex.Message);
}
