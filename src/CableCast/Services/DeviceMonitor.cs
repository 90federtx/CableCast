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

            var status = DeviceStatusResolver.Resolve(query, phones);
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
