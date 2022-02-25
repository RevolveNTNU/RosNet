using Microsoft.Extensions.Logging;

using static RosNet.MessageGeneration.Utilities;

namespace RosNet.MessageGeneration;

/// <summary>Generate C# classes for ROS Services</summary>
public class ServiceCodeGenerator : CodeGenerator
{
    private static readonly string[] Types = { "Request", "Response" };

    /// <summary>Create a <c>ServiceCodeGenerator</c> instance</summary>
    public ServiceCodeGenerator(ILogger<ServiceCodeGenerator> logger) : base(logger)
    {
    }

    /// <inheritdoc cref="CodeGenerator.FileExtension"/>
    public static new string FileExtension => "srv";
    /// <inheritdoc cref="CodeGenerator.FileName"/>
    public static new string FileName => "service";

    /// <inheritdoc/>
    protected override void GenerateSingle(string inputPath, string? outputPath, string? rosPackageName)
    {
        // If no ROS package name is provided, extract from path (it should look like `{packageName}/{msg/srv/action}/*.{msg/srv/action}`)
        rosPackageName ??= inputPath.Split(Path.DirectorySeparatorChar)[^3];

        outputPath = Path.Combine(outputPath ?? "", ResolvePackageName(rosPackageName));

        string rosMessageName = Path.GetFileNameWithoutExtension(inputPath);

        if (rosPackageName == "std_msgs" || rosMessageName.ToLower() is "time" or "duration")
        {
            Logger.LogInformation("{File} will not be generated", rosMessageName);
            return;
        }

        Logger.LogDebug("Parsing: {File}", inputPath);
        Logger.LogDebug("Output Location: {File}", outputPath);
        var f = File.ReadAllText(inputPath);
        var files = MessageParser.ParseFile(f);

        if (files.Count() != 2)
        {
            throw new MessageParserException("Unexpected number of sections. Service should have 2 sections.");
        }

        Directory.CreateDirectory(outputPath);

        foreach (var (tokens, type) in files.Zip(Types))
        {
            var parsed = new ParseResult(tokens, rosMessageName + type, rosPackageName);
            var code = GenerateCode(parsed);

            var fileOutputPath = Path.Combine(outputPath, FileExtension, Path.ChangeExtension(parsed.RosMessageName, FileExtension));
            File.WriteAllText(fileOutputPath, code);
        }
    }
}
