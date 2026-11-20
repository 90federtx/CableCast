using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using CableCast.Models;

namespace CableCast.Services;

/// <summary>Contenuto di settings.json.</summary>
public sealed class SettingsFile
{
    public int Version { get; set; } = 1;
    public string Theme { get; set; } = "Dark";
    public bool CheckUpdates { get; set; } = true;
    public string ActiveProfile { get; set; } = "";
    public Profile? Current { get; set; }
    public List<Profile> CustomProfiles { get; set; } = new();
}

/// <summary>
/// Profili (predefiniti + personalizzati) e salvataggio in settings.json.
/// Un file mancante o rovinato non è mai un errore: si riparte dai valori predefiniti.
/// </summary>
public sealed class ProfileStore : IDisposable
{
    public const string LowLatencyKey = "Profile_LowLatency";
    public const string HighQualityKey = "Profile_HighQuality";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;
    private readonly MirrorSettings _settings;
    private readonly LogService? _log;
    private readonly object _lock = new();
    private readonly TimeSpan _saveDelay;
    private System.Threading.Timer? _saveTimer;
    private bool _disposed;
    private bool _applying;

    public ObservableCollection<Profile> Profiles { get; } = new();
    public string ActiveName { get; private set; } = "";
    public event Action? ActiveChanged;
    public event Action? ThemeChanged;

    /// <summary>Tema dell'app ("Dark" o "Light"), salvato in settings.json.</summary>
    public string Theme { get; private set; } = "Dark";

    public void SetTheme(string theme)
    {
        theme = theme == "Light" ? "Light" : "Dark";
        if (theme == Theme) return;
        Theme = theme;
        ThemeChanged?.Invoke();
        Save();
    }

    /// <summary>Se true, all'avvio l'app cerca aggiornamenti su GitHub.</summary>
    public bool CheckUpdates { get; private set; } = true;

    public void SetCheckUpdates(bool value)
    {
        if (value == CheckUpdates) return;
        CheckUpdates = value;
        Save();
    }

    public ProfileStore(string path, MirrorSettings settings, LogService? log = null, TimeSpan? saveDelay = null)
    {
        _path = path;
        _settings = settings;
        _log = log;
        _saveDelay = saveDelay ?? TimeSpan.FromMilliseconds(600);
        foreach (var p in BuiltIns()) Profiles.Add(p);
        _settings.PropertyChanged += OnSettingsChanged;
    }

    public static IEnumerable<Profile> BuiltIns()
    {
        yield return new Profile
        {
            Name = Loc.Get(LowLatencyKey), BuiltIn = true,
            MaxSize = 1280, MaxFps = 60, VideoBitRateMbit = 8, AudioEnabled = true,
            TurnScreenOff = false, StayAwake = true, AlwaysOnTop = false, Fullscreen = false,
            WindowTitle = "", Gamepad = GamepadMode.Uhid,
        };
        yield return new Profile
        {
            Name = Loc.Get(HighQualityKey), BuiltIn = true,
            MaxSize = 0, MaxFps = 60, VideoBitRateMbit = 16, AudioEnabled = true,
            TurnScreenOff = false, StayAwake = true, AlwaysOnTop = false, Fullscreen = false,
            WindowTitle = "", Gamepad = GamepadMode.Uhid,
        };
    }

    public Profile Default => Profiles.First(p => p.BuiltIn);

    public bool NameTaken(string name) =>
        Profiles.Any(p => string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));

    /// <summary>Legge il file e applica le impostazioni. Non lancia mai eccezioni.</summary>
    public void Load()
    {
        SettingsFile? file = null;
        try
        {
            if (File.Exists(_path))
                file = JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(_path), Json);
        }
        catch (Exception ex)
        {
            _log?.Warn(Loc.Format("Log_SettingsCorrupt", ex.Message));
        }

        Theme = file?.Theme == "Light" ? "Light" : "Dark";
        CheckUpdates = file?.CheckUpdates ?? true;

        _applying = true;
        try
        {
            foreach (var c in (file?.CustomProfiles ?? new()).Where(c => c is not null && !string.IsNullOrWhiteSpace(c.Name)))
            {
                c.Name = c.Name.Trim();
                if (!NameTaken(c.Name)) Profiles.Add(c);
            }

            if (file?.Current is { } cur) cur.ApplyTo(_settings);
            else Default.ApplyTo(_settings);
            ActiveName = Profiles.Any(p => p.Name == file?.ActiveProfile) ? file!.ActiveProfile : Default.Name;
        }
        finally { _applying = false; }
        ActiveChanged?.Invoke();
    }

    public void Apply(Profile profile)
    {
        _applying = true;
        try { profile.ApplyTo(_settings); }
        finally { _applying = false; }
        ActiveName = profile.Name;
        ActiveChanged?.Invoke();
        Save();
    }

    /// <summary>Salva le impostazioni correnti come nuovo profilo (o aggiorna uno personalizzato esistente).</summary>
    public Profile? SaveAs(string name)
    {
        name = (name ?? "").Trim();
        if (name.Length == 0) return null;
        var existing = Profiles.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing is { BuiltIn: true }) return null;

        var p = Profile.From(name, _settings);
        if (existing is not null) Profiles[Profiles.IndexOf(existing)] = p;
        else Profiles.Add(p);
        ActiveName = p.Name;
        ActiveChanged?.Invoke();
        Save();
        return p;
    }

    public bool Delete(Profile profile)
    {
        if (profile.BuiltIn || !Profiles.Remove(profile)) return false;
        if (ActiveName == profile.Name)
        {
            ActiveName = Default.Name;
            _applying = true;
            try { Default.ApplyTo(_settings); }
            finally { _applying = false; }
            ActiveChanged?.Invoke();
        }
        Save();
        return true;
    }

    private void OnSettingsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_applying) return;
        ScheduleSave();
    }

    /// <summary>Scrittura atomica (file temporaneo + sostituzione). Non lancia mai eccezioni.</summary>
    public void Save()
    {
        lock (_lock)
        {
            _saveTimer?.Dispose();
            _saveTimer = null;
        }
        ScheduleSave(immediate: true);
    }

    private void ScheduleSave(bool immediate = false)
    {
        lock (_lock)
        {
            if (_disposed) return;
            _saveTimer?.Dispose();
            if (immediate)
            {
                _saveTimer = null;
                SaveNow();
            }
            else
            {
                _saveTimer = new System.Threading.Timer(_ => SaveNow(), null, _saveDelay, Timeout.InfiniteTimeSpan);
            }
        }
    }

    private void SaveNow()
    {
        lock (_lock)
        {
            try
            {
                var file = new SettingsFile
                {
                    Theme = Theme,
                    CheckUpdates = CheckUpdates,
                    ActiveProfile = ActiveName,
                    Current = Profile.From("current", _settings),
                    CustomProfiles = Profiles.Where(p => !p.BuiltIn).ToList(),
                };
                var dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                var tmp = _path + ".tmp";
                File.WriteAllText(tmp, JsonSerializer.Serialize(file, Json));
                File.Move(tmp, _path, overwrite: true);
            }
            catch (Exception ex)
            {
                _log?.Warn(Loc.Format("Log_SettingsSaveFailed", ex.Message));
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _saveTimer?.Dispose();
            _saveTimer = null;
        }
        _settings.PropertyChanged -= OnSettingsChanged;
        SaveNow();
    }
}
