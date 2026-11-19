using CableCast.Models;
using CableCast.Services;

namespace CableCast.ViewModels;

/// <summary>Testi italiani per ciascuno stato del telefono (dalle risorse).</summary>
public static class DeviceTextMapper
{
    public static (string Title, string Text) Map(DeviceStatus s) => s.State switch
    {
        DeviceState.Searching => (Loc.Get("Home_Searching_Title"), Loc.Get("Home_Searching_Text")),
        DeviceState.AdbUnavailable => (Loc.Get("Home_AdbUnavailable_Title"), Loc.Format("Home_AdbUnavailable_Text", AppInfo.Name, s.Detail ?? "-")),
        DeviceState.NoDevice => (Loc.Get("Home_NoDevice_Title"), Loc.Get("Home_NoDevice_Text")),
        DeviceState.UsbDebuggingOff => (Loc.Get("Home_UsbDebuggingOff_Title"), Loc.Format("Home_UsbDebuggingOff_Text", s.Detail ?? "?")),
        DeviceState.Unauthorized => (Loc.Get("Home_Unauthorized_Title"), Loc.Get("Home_Unauthorized_Text")),
        DeviceState.Offline => (Loc.Get("Home_Offline_Title"), Loc.Format("Home_Offline_Text", s.Detail ?? "offline")),
        DeviceState.Ready => (Loc.Get("Home_Ready_Title"), Loc.Format("Home_Ready_Text", s.Device?.DisplayName ?? "")),
        _ => ("", ""),
    };

    public static bool NeedsWizard(DeviceState state) =>
        state is DeviceState.NoDevice or DeviceState.UsbDebuggingOff or DeviceState.Unauthorized or DeviceState.Offline;
}
