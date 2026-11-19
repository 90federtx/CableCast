using CableCast.Models;

namespace CableCast.Services;

/// <summary>Trasforma output di adb e sonda USB in uno stato chiaro. Pura e testabile.</summary>
public static class DeviceStatusResolver
{
    public static DeviceStatus Resolve(AdbQueryResult query, IReadOnlyList<string> usbPhonesSeenByWindows)
    {
        if (!query.Ok) return new DeviceStatus(DeviceState.AdbUnavailable, null, query.Error);

        // Si preferisce un dispositivo via cavo, poi qualunque altro.
        var ordered = query.Devices.OrderByDescending(d => d.IsUsb).ToList();

        var ready = ordered.FirstOrDefault(d => d.State == "device");
        if (ready is not null) return new DeviceStatus(DeviceState.Ready, ready);

        var unauthorized = ordered.FirstOrDefault(d => d.State == "unauthorized");
        if (unauthorized is not null) return new DeviceStatus(DeviceState.Unauthorized, unauthorized);

        var other = ordered.FirstOrDefault();
        if (other is not null) return new DeviceStatus(DeviceState.Offline, other, other.State);

        return usbPhonesSeenByWindows.Count > 0
            ? new DeviceStatus(DeviceState.UsbDebuggingOff, null, usbPhonesSeenByWindows[0])
            : new DeviceStatus(DeviceState.NoDevice);
    }
}
