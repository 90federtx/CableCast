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

    public static string Diagnostic(AdbDevice device, DeviceStatus status)
    {
        if (!IsXiaomiFamily(device.Manufacturer, device.Model) && !IsXiaomiFamily(device.Product))
            return "";

        if (status.State == DeviceState.Unauthorized)
            return "Dispositivo Xiaomi rilevato. Sul telefono tocca Consenti al debug USB, attiva «Consenti sempre da questo computer» e verifica «Debug USB (Impostazioni sicurezza)» nelle Opzioni sviluppatore.";
        if (status.State == DeviceState.UsbDebuggingOff)
            return "Dispositivo Xiaomi rilevato. Attiva Debug USB e Debug USB (Impostazioni sicurezza), quindi imposta il cavo USB su Trasferimento file.";
        if (status.State == DeviceState.Offline)
            return "Dispositivo Xiaomi rilevato ma offline. Sblocca lo schermo, usa una porta USB diretta e disattiva temporaneamente l'uso esclusivo di altri strumenti adb.";
        if (status.State == DeviceState.NoDevice)
            return "Dispositivo Xiaomi rilevato da Windows ma non da adb. Verifica Trasferimento file, Debug USB e le autorizzazioni di sicurezza MIUI/HyperOS.";
        return "Dispositivo Xiaomi rilevato. Sono attivi i controlli specifici per MIUI/HyperOS: mantenere il telefono sbloccato durante la prima autorizzazione.";
    }
}
