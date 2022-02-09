using Microsoft.Extensions.Logging;

namespace RosNet.MessageGeneration;

public abstract class CodeGenerator
{
    protected readonly ILogger<CodeGenerator> _logger;

    public CodeGenerator(ILogger<CodeGenerator> logger)
    {
        _logger = logger;
    }
    public static string FileExtension => throw new InvalidOperationException("Accessing fileextension of abstract class");

    public static string FileName => throw new InvalidOperationException("Accessing filename of abstract class");
    protected abstract List<string> GenerateSingle(string inputPath, string? outputPath, string? rosPackageName = "");
    public List<string> Generate(string inputPath, string? outputPath, string? rosPackageName)
    {
        var fileName = (string)this.GetType().GetField("FileName")?.GetValue(null)!;
        var fileExtension = (string)this.GetType().GetField("FileExtension")?.GetValue(null)!;
        var warnings = new List<string>();

        if (Directory.Exists(inputPath))
        {
            // TODO: make this flexible depending on 
            string[] files = Directory.GetFiles(inputPath, "*" + fileExtension, SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                _logger.LogError("No files found in {fileName}!", fileName);
                return warnings;
            }
            else
            {
                _logger.LogDebug("Found {count} files in {directory}.", files.Length, fileName);
                foreach (string file in files)
                {
                    warnings.AddRange(GenerateSingle(file, outputPath, rosPackageName));
                }
            }
        }
        else if (File.Exists(inputPath))
        {
            _logger.LogDebug("Generating code for {File}", inputPath);
            warnings.AddRange(GenerateSingle(Path.GetFullPath(inputPath), outputPath, rosPackageName));
        }

        return warnings;
    }
}
