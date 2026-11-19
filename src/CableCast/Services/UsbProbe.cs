using System.Management;

namespace CableCast.Services;

/// <summary>
/// Cerca nel sistema telefoni collegati via USB anche quando il debug USB è spento
/// (in quel caso adb non li vede, ma Windows li mostra come dispositivi portatili/MTP).
/// </summary>
public interface IUsbProbe
{
    Task<IReadOnlyList<string>> FindPhonesAsync();
}

public sealed class UsbProbe : IUsbProbe
{
    public Task<IReadOnlyList<string>> FindPhonesAsync() => Task.Run<IReadOnlyList<string>>(() =>
    {
        var names = new List<string>();
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, PNPClass, Service FROM Win32_PnPEntity WHERE PNPClass = 'WPD' OR PNPClass = 'AndroidUsbDeviceClass' OR Service = 'WUDFWpdMtp'");
            foreach (ManagementBaseObject o in searcher.Get())
            {
                var name = o["Name"] as string;
                if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
            }
        }
        catch
        {
            // WMI non disponibile: non è un errore, semplicemente nessun suggerimento.
        }
        return names;
    });
}
