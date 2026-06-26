using System.Text;

namespace ConsoleFileWriterWatcher.Services;

public class FileWriterService
{
    private readonly Dictionary<string, StreamWriter> _writers = new();
    private readonly Dictionary<string, int> _dotCounts = new();
    private readonly object _lock = new();

    public void StartFile(string fileName, string outputPath)
    {
        lock (_lock)
        {
            if (_writers.ContainsKey(fileName))
                return;

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var stream = new FileStream(
                outputPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read,
                4096,
                FileOptions.WriteThrough
            );
            var writer = new StreamWriter(stream, Encoding.UTF8);

            writer.WriteLine($"Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.Flush();

            _writers[fileName] = writer;
            _dotCounts[fileName] = 0;
        }
    }

    public void WriteDot(string fileName)
    {
        lock (_lock)
        {
            if (!_writers.TryGetValue(fileName, out var writer))
                return;

            writer.Write(".");
            writer.Flush();
            _dotCounts[fileName]++;

            if (_dotCounts[fileName] >= 30)
            {
                writer.WriteLine();
                writer.Flush();
                _dotCounts[fileName] = 0;
            }
        }
    }

    public void StopFile(string fileName)
    {
        lock (_lock)
        {
            if (!_writers.TryGetValue(fileName, out var writer))
                return;

            if (_dotCounts[fileName] > 0)
            {
                writer.WriteLine();
            }

            writer.WriteLine($"Stopped: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.Flush();

            writer.Close();
            writer.Dispose();

            _writers.Remove(fileName);
            _dotCounts.Remove(fileName);
        }
    }

    public List<string> ActiveFiles()
    {
        lock (_lock)
        {
            return [.. _writers.Keys];
        }
    }

    public void StopAll()
    {
        lock (_lock)
        {
            foreach (var fileName in _writers.Keys.ToList())
            {
                StopFile(fileName);
            }
        }
    }
}
