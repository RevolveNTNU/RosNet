using Microsoft.Extensions.Logging;

namespace RosNet.MessageGeneration;

/// Generate C# Code for ROS-Message files
public class MessageCodeGenerator : CodeGenerator
{
    /// <inheritdoc cref="CodeGenerator.FileExtension"/>
    public static new string FileExtension => "msg";
    /// <inheritdoc cref="CodeGenerator.FileName"/>
    public static new string FileName => "message";

    /// <summary>Create a Message Generator</summary>
    public MessageCodeGenerator(ILogger<MessageCodeGenerator> logger) : base(logger)
    {
    }

    /// <inheritdoc/>
    protected override void GenerateSingle(string inputPath, string? outputPath, string? rosPackageName)
    {
        // If no ROS package name is provided, extract from path (it should look like `{packageName}/{msg/srv/action}/*.{msg/srv/action}`)
        rosPackageName ??= inputPath.Split(Path.DirectorySeparatorChar)[^3];

        outputPath = Path.Combine(outputPath ?? "", Utilities.ResolvePackageName(rosPackageName));

        string rosMessageName = Path.GetFileNameWithoutExtension(inputPath);

        if (rosPackageName != "std_msgs" && rosMessageName.ToLower() is not ("time" or "duration"))
        {
            Logger.LogDebug("Parsing: {File}", inputPath);
            Logger.LogDebug("Output Location: {File}", outputPath);
            var f = File.ReadAllText(inputPath);
            var listOfTokens = MessageTokenizer.Tokenize(f);

            var parsed = MessageParser.Parse(listOfTokens.Single(), rosMessageName, rosPackageName);
            var code = GenerateCode(parsed);

            Directory.CreateDirectory(outputPath);
            var fileOutputPath = Path.Combine(outputPath, FileExtension, Path.ChangeExtension(parsed.RosMessageName, FileExtension));
            File.WriteAllText(fileOutputPath, code);
        }

        Logger.LogInformation("{File} will not be generated", rosMessageName);
    }
}
