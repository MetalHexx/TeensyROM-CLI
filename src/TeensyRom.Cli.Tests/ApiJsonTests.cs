using System.Net;
using TeensyRom.Cli.Api.Generated;
using static TeensyRom.Cli.Tests.ApiTestSupport;

namespace TeensyRom.Cli.Tests;

public class ApiJsonTests
{
    private const string FindDevicesBody = """
        {
          "devices": [
            {
              "deviceId": "abc123", "isConnected": true, "deviceState": "Connected",
              "comPort": "COM3", "connectionType": "Serial", "ipAddress": "", "tcpPort": 0,
              "name": "Alpha", "fwVersion": "1.2.3", "isCompatible": true, "hardwareVariant": "TeensyRomPlus",
              "isMinimalFirmware": false, "buildTimestamp": "", "machine": "C64", "videoStandard": "PAL",
              "sdStorage": { "deviceId": "abc123", "type": "SD", "available": true, "indexExists": false },
              "usbStorage": { "deviceId": "abc123", "type": "USB", "available": false, "indexExists": false }
            },
            {
              "deviceId": "def456", "isConnected": false, "deviceState": "ConnectionLost",
              "comPort": "", "connectionType": "Tcp", "ipAddress": "10.0.0.7", "tcpPort": 2112,
              "name": "Bravo", "fwVersion": "", "isCompatible": true, "hardwareVariant": "Unknown",
              "isMinimalFirmware": false, "buildTimestamp": "", "machine": "Unknown", "videoStandard": "Unknown",
              "sdStorage": { "deviceId": "def456", "type": "SD", "available": false, "indexExists": false },
              "usbStorage": { "deviceId": "def456", "type": "USB", "available": false, "indexExists": false }
            }
          ],
          "message": "Found 2 devices"
        }
        """;

    [Fact]
    public async Task FindDevices_WithEnumNamesOnTheWire_Deserializes()
    {
        var client = ClientAnswering(HttpStatusCode.OK, FindDevicesBody);

        var response = await client.FindDevicesAsync(fullScan: false);

        var devices = response.Devices.ToList();
        devices.Should().HaveCount(2);
        devices[0].DeviceState.Should().Be(DeviceState.Connected);
        devices[0].ConnectionType.Should().Be(ConnectionType.Serial);
        devices[0].SdStorage.Type.Should().Be(TeensyStorageType.SD);
        devices[1].DeviceState.Should().Be(DeviceState.ConnectionLost);
        devices[1].ConnectionType.Should().Be(ConnectionType.Tcp);
    }

    [Fact]
    public async Task DocumentedNotFound_WithABareJsonString_KeepsTheBodyOnTheException()
    {
        var client = ClientAnswering(HttpStatusCode.NotFound, "\"No files were found.\"");

        var act = () => client.LaunchRandomAsync("abc123", TeensyStorageType.USB);

        var thrown = (await act.Should().ThrowAsync<ApiException>()).Which;
        thrown.StatusCode.Should().Be(404);
        thrown.Response.Should().Contain("No files were found.");
    }
}
