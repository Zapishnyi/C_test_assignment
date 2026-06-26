using ConsoleFileWriterWatcher.Services;

const string OutputDir = "data";

var configFile = "config/config.csv";

var writer = new FileWriterService();

// Ensure output directory exists
Directory.CreateDirectory(OutputDir);

// Helper to map config filename to output path (preserves subdirectory structure)
string MapToOutputPath(string fileName) => Path.Combine(OutputDir, fileName);

// 1. Load initial files from CSV
var config = new ConfigContentExtractService();
var currentFiles = config.Load(configFile);


// 2. Start writing to files (appends to existing files or creates new ones)
foreach (var file in currentFiles)
{
    var outputPath = MapToOutputPath(file);
    writer.StartFile(file, outputPath);
}

// 3. Handle graceful shutdown on Ctrl+C
var shutdownEvent = new ManualResetEventSlim(false);

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdownEvent.Set();
};

// 4. Write dots every second
var timer = new Timer(_ =>
{
    foreach (var file in writer.ActiveFiles())
    {
        writer.WriteDot(file);
    }
}, null, 0, 1000);

// 5. Watch CSV for changes
var watcher = new ConfigWatcherService(configFile);

watcher.Start(
    onChanged: () =>
    {
        var config = new ConfigContentExtractService();

        var current = config.Load(configFile);
        var active = writer.ActiveFiles();

        // New files added to CSV -> start writing
        foreach (var file in current)
        {
            if (!active.Contains(file))
            {
                var outputPath = MapToOutputPath(file);
                writer.StartFile(file, outputPath);
            }
        }

        // Files removed from CSV -> stop writing
        foreach (var file in active)
        {
            if (!current.Contains(file))
            {
                writer.StopFile(file);
            }
        }
    },
    onConfigMissing: () =>
    {
        writer.StopAll();
        watcher.Stop();
        timer.Dispose();
        shutdownEvent.Set();
    });

// 6. Main loop
Console.WriteLine("Console File Writer Watcher is running. Press Ctrl+C to stop.");
shutdownEvent.Wait();

var activeFiles = writer.ActiveFiles();
var stopTime = DateTime.Now;

Console.WriteLine($"\nShutting down at {stopTime:yyyy-MM-dd HH:mm:ss}...");

foreach (var file in activeFiles)
{
    Console.WriteLine($"  Stopped: {file} at {stopTime:yyyy-MM-dd HH:mm:ss}");
}

writer.StopAll();
watcher.Stop();
timer.Dispose();