using CableCast.Models;

namespace CableCast.Services;

/// <summary>Controlla periodicamente il telefono e notifica i cambi di stato. Non lancia mai eccezioni.</summary>
public sealed class DeviceMonitor : IDisposable
{
    private readonly IAdbService _adb;
    private readonly IUsbProbe _usb;
    private readonly TimeSpan _interval;
    private CancellationTokenSource? _cts;
    private int _consecutiveFailures;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private AdbDeviceInfo _deviceInfo = AdbDeviceInfo.Empty;
    private string? _deviceInfoSerial;
    private int _disconnectedCycles;
    public AdbDeviceInfo DeviceInfo => Volatile.Read(ref _deviceInfo);
    private static bool IsMetadataIncomplete(AdbDeviceInfo info) =>
        string.IsNullOrWhiteSpace(info.Manufacturer)
        || string.IsNullOrWhiteSpace(info.Model)
        || string.IsNullOrWhiteSpace(info.AndroidVersion);

    public DeviceMonitor(IAdbService adb, IUsbProbe usb, TimeSpan? interval = null)
    {
        _adb = adb;
        _usb = usb;
        _interval = interval ?? TimeSpan.FromSeconds(2);
    }

    public DeviceStatus Current { get; private set; } = DeviceStatus.Searching;

    /// <summary>Sollevato su un thread qualsiasi quando lo stato cambia.</summary>
    public event Action<DeviceStatus>? StatusChanged;

    public void Start()
    {
        if (_cts is not null) return;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                await RefreshAsync(token).ConfigureAwait(false);
                try { await Task.Delay(_interval, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }, token);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            var query = await SafeListAsync(ct).ConfigureAwait(false);

            // adb bloccato: dopo 2 errori di fila si riavvia il server una volta.
            if (!query.Ok)
            {
                if (++_consecutiveFailures == 2)
                {
                    await SafeRestartAsync(ct).ConfigureAwait(false);
                    query = await SafeListAsync(ct).ConfigureAwait(false);
                }
            }
            else _consecutiveFailures = 0;

            IReadOnlyList<string> phones = Array.Empty<string>();
            if (query.Ok && query.Devices.Count == 0)
            {
                try { phones = await _usb.FindPhonesAsync().ConfigureAwait(false); }
                catch { /* suggerimento facoltativo */ }
            }

            var ready = query.Devices.FirstOrDefault(d => d.State == "device");
            if (ready is not null &&
                (_deviceInfoSerial != ready.Serial || IsMetadataIncomplete(_deviceInfo)))
            {
                try
                {
                    var info = await _adb.GetDeviceInfoAsync(ready.Serial, ct).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(info.Manufacturer)
                        || !string.IsNullOrWhiteSpace(info.Model)
                        || !string.IsNullOrWhiteSpace(info.AndroidVersion))
                    {
                        Volatile.Write(ref _deviceInfo, info);
                        _deviceInfoSerial = ready.Serial;
                    }
                    ready = ready with
                    {
                        Manufacturer = info.Manufacturer,
                        AndroidVersion = info.AndroidVersion,
                        MiuiVersion = info.MiuiVersion,
                        HyperOsVersion = info.HyperOsVersion
                    };
                    query = AdbQueryResult.Success(query.Devices.Select(d =>
                        d.Serial == ready.Serial ? ready : d).ToList());
                }
                catch (OperationCanceledException) { throw; }
                catch { /* i metadati sono facoltativi */ }
            }

            if (!query.Devices.Any(d => d.State == "device"))
            {
                Volatile.Write(ref _deviceInfo, AdbDeviceInfo.Empty);
                _deviceInfoSerial = null;
            }

            var status = DeviceStatusResolver.Resolve(query, phones);
            if (status.IsReady)
            {
                _disconnectedCycles = 0;
            }
            else
            {
                _disconnectedCycles++;
                if (_disconnectedCycles == 3)
                {
                    await SafeRestartAsync(ct).ConfigureAwait(false);
                    query = await SafeListAsync(ct).ConfigureAwait(false);
                    status = DeviceStatusResolver.Resolve(query, phones);
                    _disconnectedCycles = 0;
                }
            }
            if (status != Current)
            {
                Current = status;
                StatusChanged?.Invoke(status);
            }
        }
        catch (OperationCanceledException) { }
        finally { _gate.Release(); }
    }

    private async Task<AdbQueryResult> SafeListAsync(CancellationToken ct)
    {
        try { return await _adb.ListDevicesAsync(ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex) { return AdbQueryResult.Failure(ex.Message); }
    }

    private async Task SafeRestartAsync(CancellationToken ct)
    {
        try { await _adb.RestartServerAsync(ct).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch { /* si riprova al giro successivo */ }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
