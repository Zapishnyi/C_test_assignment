namespace ConsoleFileWriterWatcher.Services;

public class ConfigWatcherService
{
    private FileSystemWatcher? _watcher;
    private readonly string _csvAbsolutePath;
    private Timer? _debounceTimer;
    private Action? _onChanged;
    private Action? _onConfigMissing;

    public ConfigWatcherService(string csvRelativePath)
    {
        _csvAbsolutePath = Path.GetFullPath(csvRelativePath);
    }

    public void Start(Action onChanged, Action? onConfigMissing = null)
    {
        _onChanged = onChanged;
        _onConfigMissing = onConfigMissing;

        var directory = Path.GetDirectoryName(_csvAbsolutePath) ?? ".";
        var fileName = Path.GetFileName(_csvAbsolutePath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _watcher = new FileSystemWatcher(directory, fileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        _watcher.Changed += OnCsvChanged;
        _watcher.Created += OnCsvChanged;
        _watcher.Deleted += OnCsvDeleted;
        _watcher.Renamed += OnCsvRenamed;
    }

    private void OnConfigMissing()
    {
        Console.Error.WriteLine("ERROR: Config file is missing. Terminating program.");
        _onConfigMissing?.Invoke();
    }

    private void OnCsvDeleted(object sender, FileSystemEventArgs e)
    {
        OnConfigMissing();
    }

    private void OnCsvRenamed(object sender, RenamedEventArgs e)
    {
        OnConfigMissing();
    }

    private void OnCsvChanged(object sender, FileSystemEventArgs e)
    {
        // Debounce: wait 200ms after last change before triggering
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ =>
        {
            try
            {
                _onChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Watcher error: {ex.Message}");
            }
        }, null, 200, Timeout.Infinite);
    }

    public void Stop()
    {
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
    }
}