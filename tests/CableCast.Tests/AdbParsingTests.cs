using CableCast.Models;
using CableCast.Services;
using Xunit;

namespace CableCast.Tests;

public class AdbOutputParserTests
{
    [Fact]
    public void Empty_output_gives_no_devices()
    {
        Assert.Empty(AdbOutputParser.ParseDevices(""));
        Assert.Empty(AdbOutputParser.ParseDevices(null));
        Assert.Empty(AdbOutputParser.ParseDevices("List of devices attached\r\n\r\n"));
    }

    [Fact]
    public void Parses_ready_device_with_details()
    {
        const string o = "List of devices attached\r\nR58M12ABCDE        device usb:1-1 product:beyond1 model:SM_G973F device:beyond1 transport_id:3\r\n\r\n";
        var d = Assert.Single(AdbOutputParser.ParseDevices(o));
        Assert.Equal("R58M12ABCDE", d.Serial);
        Assert.Equal("device", d.State);
        Assert.Equal("SM_G973F", d.Model);
        Assert.Equal("beyond1", d.Product);
        Assert.Equal("3", d.TransportId);
        Assert.True(d.IsUsb);
    }

    [Fact]
    public void Parses_unauthorized_and_offline()
    {
        const string o = "List of devices attached\nAAA unauthorized usb:1-1 transport_id:1\nBBB offline transport_id:2\n";
        var list = AdbOutputParser.ParseDevices(o);
        Assert.Equal(2, list.Count);
        Assert.Equal("unauthorized", list[0].State);
        Assert.Equal("offline", list[1].State);
    }

    [Fact]
    public void Parses_no_permissions_state()
    {
        const string o = "List of devices attached\nAAA no permissions (user in plugdev group; are your udev rules wrong?); see [http://developer.android.com/tools/device.html] usb:1-1 transport_id:4\n";
        var d = Assert.Single(AdbOutputParser.ParseDevices(o));
        Assert.Equal("no permissions", d.State);
    }

    [Fact]
    public void Ignores_daemon_startup_lines()
    {
        const string o = "* daemon not running; starting now at tcp:5037\n* daemon started successfully\nList of devices attached\nAAA device model:Pixel_7 transport_id:1\n";
        var d = Assert.Single(AdbOutputParser.ParseDevices(o));
        Assert.Equal("Pixel_7", d.Model);
    }

    [Fact]
    public void Wifi_and_emulator_serials_are_not_usb()
    {
        var list = AdbOutputParser.ParseDevices("List of devices attached\n192.168.1.5:5555 device\nemulator-5554 device\n");
        Assert.Equal(2, list.Count);
        Assert.All(list, d => Assert.False(d.IsUsb));
    }

    [Fact]
    public void ParseSingleValue_returns_first_non_empty_line()
    {
        Assert.Equal("Pixel 7", AdbOutputParser.ParseSingleValue("\r\n Pixel 7 \r\nother"));
        Assert.Null(AdbOutputParser.ParseSingleValue("  \r\n"));
    }
}

public class DeviceStatusResolverTests
{
    private static AdbDevice Dev(string serial, string state) => new(serial, state, null, null, null);
    private static readonly IReadOnlyList<string> NoUsb = Array.Empty<string>();

    [Fact]
    public void Adb_failure_is_AdbUnavailable()
    {
        var s = DeviceStatusResolver.Resolve(AdbQueryResult.Failure("boom"), NoUsb);
        Assert.Equal(DeviceState.AdbUnavailable, s.State);
        Assert.Equal("boom", s.Detail);
    }

    [Fact]
    public void No_devices_and_no_usb_is_NoDevice()
    {
        var s = DeviceStatusResolver.Resolve(AdbQueryResult.Success(Array.Empty<AdbDevice>()), NoUsb);
        Assert.Equal(DeviceState.NoDevice, s.State);
    }

    [Fact]
    public void No_adb_devices_but_phone_seen_by_windows_is_UsbDebuggingOff()
    {
        var s = DeviceStatusResolver.Resolve(AdbQueryResult.Success(Array.Empty<AdbDevice>()), new[] { "Galaxy S10" });
        Assert.Equal(DeviceState.UsbDebuggingOff, s.State);
        Assert.Equal("Galaxy S10", s.Detail);
    }

    [Fact]
    public void Unauthorized_state()
    {
        var s = DeviceStatusResolver.Resolve(AdbQueryResult.Success(new[] { Dev("A", "unauthorized") }), NoUsb);
        Assert.Equal(DeviceState.Unauthorized, s.State);
    }

    [Fact]
    public void Offline_state_carries_adb_state()
    {
        var s = DeviceStatusResolver.Resolve(AdbQueryResult.Success(new[] { Dev("A", "offline") }), NoUsb);
        Assert.Equal(DeviceState.Offline, s.State);
        Assert.Equal("offline", s.Detail);
    }

    [Fact]
    public void Ready_state_and_usb_is_preferred_over_wifi()
    {
        var s = DeviceStatusResolver.Resolve(
            AdbQueryResult.Success(new[] { Dev("192.168.1.5:5555", "device"), Dev("USB123", "device") }), NoUsb);
        Assert.Equal(DeviceState.Ready, s.State);
        Assert.Equal("USB123", s.Device!.Serial);
        Assert.True(s.IsReady);
    }

    [Fact]
    public void Ready_wins_over_unauthorized()
    {
        var s = DeviceStatusResolver.Resolve(
            AdbQueryResult.Success(new[] { Dev("A", "unauthorized"), Dev("B", "device") }), NoUsb);
        Assert.Equal(DeviceState.Ready, s.State);
    }

    [Fact]
    public void Selected_ready_device_wins_over_default_usb_preference()
    {
        var s = DeviceStatusResolver.Resolve(
            AdbQueryResult.Success(new[]
            {
                Dev("USB123", "device"),
                Dev("192.168.1.5:5555", "device")
            }),
            NoUsb,
            "192.168.1.5:5555");

        Assert.Equal("192.168.1.5:5555", s.Device!.Serial);
        Assert.Equal(2, s.Devices.Count);
    }
}

public class XiaomiCompatibilityTests
{
    [Theory]
    [InlineData("Xiaomi", "2201116SG", true)]
    [InlineData("Redmi", "Note 11 Pro", true)]
    [InlineData("POCO", "M5", true)]
    [InlineData("Samsung", "Galaxy S24", false)]
    [InlineData(null, null, false)]
    public void ManufacturerDetection(string? manufacturer, string? model, bool expected)
        => Assert.Equal(expected, XiaomiCompatibility.IsXiaomiFamily(manufacturer, model));

    [Fact]
    public void UnauthorizedProducesSecurityInstructions()
    {
        var device = new AdbDevice("USB123", "unauthorized", "Redmi Note 11", "xaga", null, "Redmi");
        var status = new DeviceStatus(DeviceState.Unauthorized, device);
        var text = XiaomiCompatibility.Diagnostic(device, status);
        Assert.Contains("Debug USB", text);
        Assert.Contains("Consenti", text);
    }

    [Fact]
    public void ParsesXiaomiPropertiesAndAndroidVersion()
    {
        var text = """
[ro.product.manufacturer]: [Xiaomi]
[ro.product.model]: [22101320G]
[ro.build.version.release]: [14]
[ro.miui.ui.version.name]: [V14]
[ro.mi.os.version.name]: [1.0]
""";
        var info = AdbOutputParser.ParseDeviceInfo(text);
        Assert.Equal("Xiaomi", info.Manufacturer);
        Assert.Equal("22101320G", info.Model);
        Assert.Equal("14", info.AndroidVersion);
        Assert.Equal("V14", info.MiuiVersion);
        Assert.Equal("1.0", info.HyperOsVersion);
    }

    [Fact]
    public void XiaomiUsbDebuggingOffDiagnosticUsesWindowsDeviceName()
    {
        var status = new DeviceStatus(DeviceState.UsbDebuggingOff, null, "Xiaomi 14");
        var diagnostics = DiagnosticsBuilder.Build(status, new MirrorSettings(), "Profilo", "");

        Assert.Contains("Dispositivo Xiaomi/Redmi/POCO rilevato da Windows", diagnostics);
    }

    [Fact]
    public void XiaomiInjectEventsDiagnosticIncludesSupportedSecuritySettingGuidance()
    {
        var device = new AdbDevice("USB123", "device", "Redmi Note 11", "redmi", null, "Xiaomi");
        var status = new DeviceStatus(DeviceState.Ready, device);
        var diagnostics = DiagnosticsBuilder.Build(status, new MirrorSettings(), "Profilo",
            "Injecting input events requires INJECT_EVENTS permission");

        Assert.Contains("INJECT_EVENTS", diagnostics);
        Assert.Contains("Debug USB (Impostazioni sicurezza)", diagnostics);
        Assert.Contains("riavviare il telefono", diagnostics);
    }

    [Fact]
    public void HyperOsDiagnosticIsOnlyAddedWhenVersionWasDetected()
    {
        var device = new AdbDevice("USB123", "device", "POCO F6", "poco", null, "Xiaomi");
        var status = new DeviceStatus(DeviceState.Ready, device);
        var info = new AdbDeviceInfo("Xiaomi", "POCO F6", "15", null, "2.0");

        var diagnostics = DiagnosticsBuilder.Build(status, new MirrorSettings(), "Profilo", "",
            deviceInfo: info);

        Assert.Contains("Rilevata HyperOS 2.0", diagnostics);
        Assert.Contains("non può determinare automaticamente", diagnostics);
    }
}

public class DeviceMonitorReconnectTests
{
    private sealed class FailingAdb : IAdbService
    {
        public int RestartCount { get; private set; }
        public Task<AdbQueryResult> ListDevicesAsync(CancellationToken ct = default) =>
            Task.FromResult(AdbQueryResult.Failure("adb offline"));
        public Task<bool> RestartServerAsync(CancellationToken ct = default)
        {
            RestartCount++;
            return Task.FromResult(true);
        }

        public class DeviceMonitorSelectionTests
        {
            private sealed class NoUsb : IUsbProbe
            {
                public Task<IReadOnlyList<string>> FindPhonesAsync() =>
                    Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }

            private sealed class MultiDeviceAdb : IAdbService
            {
                public Task<AdbQueryResult> ListDevicesAsync(CancellationToken ct = default) =>
                    Task.FromResult(AdbQueryResult.Success(new[]
                    {
                        new AdbDevice("USB_A", "device", "Redmi Note 11", null, null),
                        new AdbDevice("192.168.1.10:5555", "device", "Pixel 8", null, null)
                    }));

                public Task<bool> RestartServerAsync(CancellationToken ct = default) =>
                    Task.FromResult(true);

                public Task<AdbDeviceInfo> GetDeviceInfoAsync(string serial, CancellationToken ct = default) =>
                    Task.FromResult(new AdbDeviceInfo(
                        serial == "USB_A" ? "Redmi" : "Google",
                        serial == "USB_A" ? "Redmi Note 11" : "Pixel 8",
                        "15", null, null));
            }

            [Fact]
            public async Task SelectionChangesActiveDeviceAndInvalidatesPreviousMetadata()
            {
                using var monitor = new DeviceMonitor(new MultiDeviceAdb(), new NoUsb());
                await monitor.RefreshAsync();

                Assert.Equal("USB_A", monitor.Current.Device!.Serial);
                Assert.Equal(2, monitor.Current.Devices.Count);
                Assert.Equal("Redmi", monitor.DeviceInfo.Manufacturer);

                monitor.SelectDevice("192.168.1.10:5555");
                Assert.Equal(AdbDeviceInfo.Empty, monitor.GetDeviceInfoFor("USB_A"));
                await monitor.RefreshAsync();

                Assert.Equal("192.168.1.10:5555", monitor.Current.Device!.Serial);
                Assert.Equal("Google", monitor.DeviceInfo.Manufacturer);
            }
        }

        public class DeviceMonitorRecoveryTests
        {
            private sealed class RecoveringAdb : IAdbService
            {
                public int Restarts { get; private set; }

                public Task<AdbQueryResult> ListDevicesAsync(CancellationToken ct = default) =>
                    Task.FromResult(Restarts == 0
                        ? AdbQueryResult.Failure("adb offline")
                        : AdbQueryResult.Success(new[]
                        {
                            new AdbDevice("USB123", "device", "Pixel", null, null)
                        }));

                public Task<bool> RestartServerAsync(CancellationToken ct = default)
                {
                    Restarts++;
                    return Task.FromResult(true);
                }

                public Task<AdbDeviceInfo> GetDeviceInfoAsync(string serial, CancellationToken ct = default) =>
                    Task.FromResult(AdbDeviceInfo.Empty);
            }

            private sealed class NoUsb : IUsbProbe
            {
                public Task<IReadOnlyList<string>> FindPhonesAsync() =>
                    Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
            }

            [Fact]
            public async Task MonitorRecoversAndReportsDeviceAfterAdbRestart()
            {
                var adb = new RecoveringAdb();
                using var monitor = new DeviceMonitor(adb, new NoUsb());

                await monitor.RefreshAsync();
                Assert.Equal(DeviceState.AdbUnavailable, monitor.Current.State);

                await monitor.RefreshAsync();

                Assert.Equal(1, adb.Restarts);
                Assert.Equal(DeviceState.Ready, monitor.Current.State);
                Assert.Equal("USB123", monitor.Current.Device!.Serial);
            }
        }
        public Task<AdbDeviceInfo> GetDeviceInfoAsync(string serial, CancellationToken ct = default) =>
            Task.FromResult(AdbDeviceInfo.Empty);
    }

    private sealed class NoUsb : IUsbProbe
    {
        public Task<IReadOnlyList<string>> FindPhonesAsync() =>
            Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    }

    [Fact]
    public async Task RepeatedUnavailableStateAttemptsAutomaticRecovery()
    {
        var adb = new FailingAdb();
        using var monitor = new DeviceMonitor(adb, new NoUsb(), TimeSpan.FromMilliseconds(1));
        await monitor.RefreshAsync();
        await monitor.RefreshAsync();
        await monitor.RefreshAsync();
        await monitor.RefreshAsync();
        Assert.True(adb.RestartCount >= 1);
    }

    private sealed class MetadataAdb : IAdbService
    {
        public int MetadataCalls { get; private set; }

        public Task<AdbQueryResult> ListDevicesAsync(CancellationToken ct = default) =>
            Task.FromResult(AdbQueryResult.Success(new[]
            {
                new AdbDevice("USB123", "device", "Redmi Note 11", null, null)
            }));

        public Task<bool> RestartServerAsync(CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<AdbDeviceInfo> GetDeviceInfoAsync(string serial, CancellationToken ct = default)
        {
            MetadataCalls++;
            var info = MetadataCalls == 1
                ? new AdbDeviceInfo("Xiaomi", "Redmi Note 11", null, null, null)
                : new AdbDeviceInfo("Xiaomi", "Redmi Note 11", "14", "V14", null);
            return Task.FromResult(info);
        }
    }

    [Fact]
    public async Task IncompleteMetadataIsNotCachedAndIsRetried()
    {
        var adb = new MetadataAdb();
        using var monitor = new DeviceMonitor(adb, new NoUsb(), TimeSpan.FromMilliseconds(1));

        await monitor.RefreshAsync();
        await monitor.RefreshAsync();

        Assert.Equal(2, adb.MetadataCalls);
        Assert.Equal("14", monitor.DeviceInfo.AndroidVersion);
    }

    private sealed class SwitchingAdb : IAdbService
    {
        private int _listCalls;

        public Task<AdbQueryResult> ListDevicesAsync(CancellationToken ct = default)
        {
            var device = Interlocked.Increment(ref _listCalls) == 1
                ? new AdbDevice("DEVICE_A", "device", "Xiaomi 13", null, null)
                : new AdbDevice("DEVICE_B", "device", "Pixel 8", null, null);
            return Task.FromResult(AdbQueryResult.Success(new[] { device }));
        }

        public Task<bool> RestartServerAsync(CancellationToken ct = default) =>
            Task.FromResult(true);

        public Task<AdbDeviceInfo> GetDeviceInfoAsync(string serial, CancellationToken ct = default) =>
            Task.FromResult(serial == "DEVICE_A"
                ? new AdbDeviceInfo("Xiaomi", "Xiaomi 13", "13", "MIUI 14", null)
                : AdbDeviceInfo.Empty);
    }

    [Fact]
    public async Task Device_switch_invalidates_old_metadata_when_new_query_fails()
    {
        var adb = new SwitchingAdb();
        using var monitor = new DeviceMonitor(adb, new NoUsb());

        await monitor.RefreshAsync();
        Assert.Equal("MIUI 14", monitor.GetDeviceInfoFor("DEVICE_A").MiuiVersion);

        await monitor.RefreshAsync();

        var status = monitor.Current;
        var metadata = monitor.GetDeviceInfoFor(status.Device!.Serial);
        var diagnostics = DiagnosticsBuilder.Build(status, new MirrorSettings(), "Profilo", "",
            deviceInfo: metadata);
        Assert.Equal("DEVICE_B", status.Device.Serial);
        Assert.Equal(AdbDeviceInfo.Empty, metadata);
        Assert.DoesNotContain("Xiaomi 13", diagnostics);
        Assert.DoesNotContain("MIUI 14", diagnostics);
        Assert.DoesNotContain("Android: 13", diagnostics);
    }
}
