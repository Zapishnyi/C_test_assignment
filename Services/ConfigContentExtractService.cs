namespace ConsoleFileWriterWatcher.Services;

public class ConfigContentExtractService
{
    public List<string> Load(string configFilePath)
    {
        if (!File.Exists(configFilePath))
            return new();

        var configurationContent = File.ReadAllLines(configFilePath);
        var result = new List<string>();

        foreach (var textLine in configurationContent)
        {
            var trimmedTextLine = textLine.Trim();

            if (string.IsNullOrEmpty(trimmedTextLine))
                continue;

            var fileNames = trimmedTextLine.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries
            );

            foreach (var fileName in fileNames)
            {
                if (!string.IsNullOrEmpty(fileName))
                    result.Add(fileName);
            }
        }

        return result;
    }
}