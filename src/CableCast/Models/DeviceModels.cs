namespace CableCast.Models;

/// <summary>Un dispositivo come riportato da "adb devices -l".</summary>
public sealed record AdbDevice(
    string Serial,
    string State,
    string? Model,
    string? Product,
    string? TransportId,
    string? Manufacturer = null,
    string? AndroidVersion = null,
    string? MiuiVersion = null,
    string? HyperOsVersion = null)
{
    /// <summary>Wi-Fi (ip:porta) ed emulatori non sono "via cavo".</summary>
    public bool IsUsb => !Serial.Contains(':') && !Serial.StartsWith("emulator-", StringComparison.OrdinalIgnoreCase);

    public string DisplayName => string.IsNullOrWhiteSpace(Model) ? Serial : Model.Replace('_', ' ');
    public string SelectionName
    {
        get
        {
            var suffix = Serial.Length <= 4 ? Serial : Serial[^4..];
            var name = string.IsNullOrWhiteSpace(Model) ? "Dispositivo Android" : Model.Replace('_', ' ');
            return $"{name} (…{suffix})";
        }
    }
}

/// <summary>Esito di una interrogazione ad adb.</summary>
public sealed record AdbQueryResult(bool Ok, IReadOnlyList<AdbDevice> Devices, string? Error)
{
    public static AdbQueryResult Success(IReadOnlyList<AdbDevice> devices) => new(true, devices, null);
    public static AdbQueryResult Failure(string error) => new(false, Array.Empty<AdbDevice>(), error);
}

public sealed record AdbDeviceInfo(
    string? Manufacturer,
    string? Model,
    string? AndroidVersion,
    string? MiuiVersion,
    string? HyperOsVersion)
{
    public static readonly AdbDeviceInfo Empty = new(null, null, null, null, null);
}

public enum DeviceState
{
    Searching,
    AdbUnavailable,
    NoDevice,
    UsbDebuggingOff,
    Unauthorized,
    Offline,
    Ready,
}

/// <summary>Stato risolto, pronto per l'interfaccia.</summary>
public sealed record DeviceStatus(DeviceState State, AdbDevice? Device = null, string? Detail = null)
{
    public static readonly DeviceStatus Searching = new(DeviceState.Searching);
    public bool IsReady => State == DeviceState.Ready;
    public IReadOnlyList<AdbDevice> Devices { get; init; } = Array.Empty<AdbDevice>();
}
