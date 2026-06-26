namespace ConsoleFileWriterWatcher.Helpers;

public class PathHelper
{
    private readonly string _outputDir;

    public PathHelper(string outputDir)
    {
        _outputDir = outputDir;
    }

    public string MapToOutputPath(string fileName)
    {
        return Path.Combine(_outputDir, fileName);
    }
}