namespace ConsoleFileWriterWatcher.Services;

public class ConfigWatcherService
{
    private FileSystemWatcher? _watcher;
    private readonly string _csvAbsolutePath;
    private Timer? _debounceTimer;
    private Action? _onChanged;
    private Action _onConfigMissing = null!;

    public ConfigWatcherService(string csvRelativePath)
    {
        _csvAbsolutePath = Path.GetFullPath(csvRelativePath);
    }

    public void Start(Action onChanged, Action onConfigMissing)
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
            NotifyFilter =
                NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.FileName,
            EnableRaisingEvents = true,
        };

        _watcher.Changed += OnCsvChanged;
        _watcher.Created += OnCsvChanged;
        _watcher.Deleted += OnCsvMissing;
        _watcher.Renamed += OnCsvMissing;
    }

    private void OnCsvMissing(object sender, EventArgs e)
    {
        Console.Error.WriteLine("ERROR: Config file is missing. Terminating program.");
        _onConfigMissing();
    }

    private void OnCsvChanged(object sender, FileSystemEventArgs e)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(
            _ =>
            {
                try
                {
                    _onChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Watcher error: {ex.Message}");
                }
            },
            null,
            200,
            Timeout.Infinite
        );
    }

    public void Stop()
    {
        _watcher?.Dispose();
        _debounceTimer?.Dispose();
    }
}
