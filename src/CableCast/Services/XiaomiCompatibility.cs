using CableCast.Models;

namespace CableCast.Services;

public static class XiaomiCompatibility
{
    public static bool IsXiaomiFamily(string? manufacturer, string? model = null)
    {
        var text = $"{manufacturer} {model}".Trim();
        if (text.Length == 0) return false;
        return text.Contains("xiaomi", StringComparison.OrdinalIgnoreCase)
            || text.Contains("redmi", StringComparison.OrdinalIgnoreCase)
            || text.Contains("poco", StringComparison.OrdinalIgnoreCase);
    }

    public static string Diagnostic(AdbDevice? device, DeviceStatus status, string? detectedDeviceName = null)
    {
        if (!IsXiaomiFamily(device?.Manufacturer, device?.Model)
            && !IsXiaomiFamily(null, device?.Product)
            && !IsXiaomiFamily(null, detectedDeviceName))
            return "";

        if (status.State == DeviceState.Unauthorized)
            return Loc.Get("Xiaomi_Unauthorized");
        if (status.State == DeviceState.UsbDebuggingOff)
            return Loc.Get("Xiaomi_UsbDebuggingOff");
        if (status.State == DeviceState.Offline)
            return Loc.Get("Xiaomi_Offline");
        if (status.State == DeviceState.NoDevice)
            return Loc.Get("Xiaomi_NoDevice");
        return Loc.Get("Xiaomi_Default");
    }
}
