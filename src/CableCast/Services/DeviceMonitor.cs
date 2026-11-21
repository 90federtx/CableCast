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
    private MetadataSnapshot _metadata = new(null, AdbDeviceInfo.Empty);
    private string? _selectedSerial;
    private int _disconnectedCycles;
    private readonly LogService? _log;
    private string? _lastAdbError;
    private sealed record MetadataSnapshot(string? Serial, AdbDeviceInfo Info);

    public AdbDeviceInfo DeviceInfo => GetDeviceInfoFor(Current.Device?.Serial);
    public string? SelectedSerial => Volatile.Read(ref _selectedSerial);

    public AdbDeviceInfo GetDeviceInfoFor(string? serial)
    {
        var metadata = Volatile.Read(ref _metadata);
        return serial is not null && string.Equals(metadata.Serial, serial, StringComparison.Ordinal)
            ? metadata.Info
            : AdbDeviceInfo.Empty;
    }

    public void SelectDevice(string? serial)
    {
        if (string.IsNullOrWhiteSpace(serial) ||
            !Current.Devices.Any(d => d.State == "device" && d.Serial == serial))
            return;

        if (string.Equals(Volatile.Read(ref _selectedSerial), serial, StringComparison.Ordinal))
            return;

        Volatile.Write(ref _selectedSerial, serial);
        Volatile.Write(ref _metadata, new MetadataSnapshot(serial, AdbDeviceInfo.Empty));
    }
    private static bool IsMetadataIncomplete(AdbDeviceInfo info) =>
        string.IsNullOrWhiteSpace(info.Manufacturer)
        || string.IsNullOrWhiteSpace(info.Model)
        || string.IsNullOrWhiteSpace(info.AndroidVersion);

    public DeviceMonitor(IAdbService adb, IUsbProbe usb, TimeSpan? interval = null, LogService? log = null)
    {
        _adb = adb;
        _usb = usb;
        _interval = interval ?? TimeSpan.FromSeconds(2);
        _log = log;
    }

    public DeviceStatus Current { get; private set; } = DeviceStatus.Searching;

    /// <summary>Sollevato su un thread qualsiasi quando lo stato cambia.</summary>
    public event Action<DeviceStatus>? StatusChanged;

    public void Start()
    {
        if (_cts is not null) return;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _log?.Info(Loc.Get("Log_AdbConnectionAttempt"));
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
            if (query.Ok)
            {
                _lastAdbError = null;
                foreach (var device in query.Devices)
                    _log?.RegisterSensitiveValue(device.Serial);
            }
            else if (!string.Equals(_lastAdbError, query.Error, StringComparison.Ordinal))
            {
                _lastAdbError = query.Error;
                _log?.Warn(Loc.Format("Log_AdbError", query.Error ?? "-"));
            }

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

            var ready = query.Devices.FirstOrDefault(d => d.State == "device"
                    && string.Equals(d.Serial, SelectedSerial, StringComparison.Ordinal))
                ?? query.Devices.FirstOrDefault(d => d.State == "device");
            if (ready is not null)
            {
                var metadata = Volatile.Read(ref _metadata);
                if (!string.Equals(metadata.Serial, ready.Serial, StringComparison.Ordinal))
                {
                    metadata = new MetadataSnapshot(ready.Serial, AdbDeviceInfo.Empty);
                    Volatile.Write(ref _metadata, metadata);
                }

                if (IsMetadataIncomplete(metadata.Info))
                {
                    try
                    {
                        var info = await _adb.GetDeviceInfoAsync(ready.Serial, ct).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(info.Manufacturer)
                            || !string.IsNullOrWhiteSpace(info.Model)
                            || !string.IsNullOrWhiteSpace(info.AndroidVersion))
                        {
                            if (SelectedSerial is null || string.Equals(SelectedSerial, ready.Serial, StringComparison.Ordinal))
                            {
                                metadata = new MetadataSnapshot(ready.Serial, info);
                                Volatile.Write(ref _metadata, metadata);
                            }
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
            }

            if (!query.Devices.Any(d => d.State == "device"))
            {
                Volatile.Write(ref _metadata, new MetadataSnapshot(null, AdbDeviceInfo.Empty));
            }

            var status = DeviceStatusResolver.Resolve(query, phones, SelectedSerial);
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
                    status = DeviceStatusResolver.Resolve(query, phones, SelectedSerial);
                    _disconnectedCycles = 0;
                }
            }
            if (status.IsReady)
            {
                var serial = status.Device!.Serial;
                Volatile.Write(ref _selectedSerial, serial);
                var metadata = Volatile.Read(ref _metadata);
                if (!string.Equals(metadata.Serial, serial, StringComparison.Ordinal))
                    Volatile.Write(ref _metadata, new MetadataSnapshot(serial, AdbDeviceInfo.Empty));
            }
            else
            {
                Volatile.Write(ref _selectedSerial, null);
            }

            var changed = status.State != Current.State
                || status.Device != Current.Device
                || status.Detail != Current.Detail
                || !status.Devices.SequenceEqual(Current.Devices);
            var previous = Current;
            Current = status;
            if (changed)
            {
                if (!previous.IsReady && status.IsReady)
                    _log?.Info(Loc.Format("Log_DeviceConnected", status.Device!.DisplayName));
                else if (previous.IsReady && !status.IsReady)
                    _log?.Warn(Loc.Get("Log_DeviceDisconnected"));
                else if (previous.IsReady && status.IsReady
                    && previous.Device?.Serial != status.Device?.Serial)
                    _log?.Info(Loc.Format("Log_DeviceSelected", status.Device!.DisplayName));
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
        _log?.Info(Loc.Get("Log_AdbRestartAttempt"));
        try
        {
            if (!await _adb.RestartServerAsync(ct).ConfigureAwait(false))
                _log?.Warn(Loc.Get("Log_AdbRestartFailed"));
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _log?.Warn(Loc.Format("Log_AdbRestartError", ex.Message));
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
