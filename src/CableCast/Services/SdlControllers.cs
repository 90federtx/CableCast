using System.IO;
using System.Runtime.InteropServices;

namespace CableCast.Services;

/// <summary>Elenca i controller collegati usando SDL3.dll fornita con scrcpy. Non lancia mai eccezioni.</summary>
public static class SdlControllers
{
    [StructLayout(LayoutKind.Sequential)]
    private struct SdlGuid
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] Data;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate bool InitD(uint flags);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void QuitD();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr GetJoysticksD(out int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate SdlGuid GetGuidD(uint id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr GetNameD(uint id);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void FreeD(IntPtr p);

    private const uint InitJoystick = 0x00000200;

    public static IReadOnlyList<ControllerInfo> Detect(LogService? log = null)
    {
        var result = new List<ControllerInfo>();
        IntPtr lib = IntPtr.Zero;
        try
        {
            var path = Path.Combine(AppInfo.ScrcpyDir, "SDL3.dll");
            if (!File.Exists(path)) return result;
            lib = NativeLibrary.Load(path);

            T Fn<T>(string name) where T : Delegate =>
                Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(lib, name));

            var init = Fn<InitD>("SDL_Init");
            var quit = Fn<QuitD>("SDL_Quit");
            var getJoysticks = Fn<GetJoysticksD>("SDL_GetJoysticks");
            var getGuid = Fn<GetGuidD>("SDL_GetJoystickGUIDForID");
            var getName = Fn<GetNameD>("SDL_GetJoystickNameForID");
            var free = Fn<FreeD>("SDL_free");

            if (!init(InitJoystick)) return result;
            try
            {
                var ids = getJoysticks(out var count);
                if (ids == IntPtr.Zero) return result;
                try
                {
                    for (var i = 0; i < count; i++)
                    {
                        var id = (uint)Marshal.ReadInt32(ids, i * 4);
                        var g = getGuid(id).Data;
                        if (g is null || g.Length != 16) continue;
                        var name = Marshal.PtrToStringUTF8(getName(id)) ?? "Controller";
                        result.Add(new ControllerInfo(Convert.ToHexString(g).ToLowerInvariant(), name));
                    }
                }
                finally { free(ids); }
            }
            finally { quit(); }
        }
        catch (Exception ex)
        {
            log?.Warn(Loc.Format("Log_ControllerDetectFailed", ex.Message));
        }
        finally
        {
            // SDL3.dll resta caricata nel processo: scrcpy ne usa una copia propria nel suo processo.
        }
        return result;
    }
}
