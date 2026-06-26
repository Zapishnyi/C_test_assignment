using ConsoleFileWriterWatcher.Services;
using ConsoleFileWriterWatcher.Helpers;

const string OutputDir = "data";
var configFile = "config/config.csv";

var writer = new FileWriterService();
var pathHelper = new PathHelper(OutputDir);

Directory.CreateDirectory(OutputDir);

var config = new ConfigContentExtractService();
var currentFiles = config.Load(configFile);

var watcher = new ConfigWatcherService(configFile);

foreach (var file in currentFiles)
{
    var outputPath = pathHelper.MapToOutputPath(file);
    writer.StartFile(file, outputPath);
}

var shutdownEvent = new ManualResetEventSlim(false);

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdownEvent.Set();
};

var timer = new Timer(_ =>
{
    foreach (var file in writer.ActiveFiles())
    {
        writer.WriteDot(file);
    }
}, null, 0, 1000);

watcher.Start(
    onChanged: () =>
    {
        var config = new ConfigContentExtractService();

        var current = config.Load(configFile);
        var active = writer.ActiveFiles();

        foreach (var file in current)
        {
            if (!active.Contains(file))
            {
                var outputPath = pathHelper.MapToOutputPath(file);
                writer.StartFile(file, outputPath);
            }
        }

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