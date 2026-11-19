namespace CableCast.Models;

/// <summary>Un dispositivo come riportato da "adb devices -l".</summary>
public sealed record AdbDevice(string Serial, string State, string? Model, string? Product, string? TransportId)
{
    /// <summary>Wi-Fi (ip:porta) ed emulatori non sono "via cavo".</summary>
    public bool IsUsb => !Serial.Contains(':') && !Serial.StartsWith("emulator-", StringComparison.OrdinalIgnoreCase);

    public string DisplayName => string.IsNullOrWhiteSpace(Model) ? Serial : Model.Replace('_', ' ');
}

/// <summary>Esito di una interrogazione ad adb.</summary>
public sealed record AdbQueryResult(bool Ok, IReadOnlyList<AdbDevice> Devices, string? Error)
{
    public static AdbQueryResult Success(IReadOnlyList<AdbDevice> devices) => new(true, devices, null);
    public static AdbQueryResult Failure(string error) => new(false, Array.Empty<AdbDevice>(), error);
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
}
