using Microsoft.Extensions.Logging;

using static RosNet.MessageGeneration.Utilities;

namespace RosNet.MessageGeneration;

public class ServiceCodeGenerator : CodeGenerator
{
    private static readonly string[] Types = { "Request", "Response" };

    public ServiceCodeGenerator(ILogger<ServiceCodeGenerator> logger) : base(logger)
    {
    }

    public static new string FileExtension => ".srv";
    public static new string FileName => "service";

    protected override List<string> GenerateSingle(string inPath, string? outPath, string? rosPackageName = "")
    {
        // If no ROS package name is provided, extract from path
        rosPackageName ??= inPath.Split(Path.DirectorySeparatorChar)[^3];
        outPath = Path.Combine(outPath ?? "", ResolvePackageName(rosPackageName));

        string inFileName = Path.GetFileNameWithoutExtension(inPath);


        _logger.LogDebug("Parsing: {file}", inPath);
        _logger.LogDebug("Output Location: {file}", outPath);

        using var tokenizer = new MessageTokenizer(inPath, new HashSet<string>(BuiltInTypesMapping.Keys));
        var listsOfTokens = tokenizer.Tokenize();

        if (listsOfTokens.Count != 2)
        {
            throw new MessageParserException("Unexpected number of sections. Service should have 2 sections.");
        }

        var warnings = new List<string>();

        for (int i = 0; i < listsOfTokens.Count; i++)
        {
            List<MessageToken> tokens = listsOfTokens[i];

            // Service is made up of request and response
            string className = inFileName + Types[i];

            MessageParser parser = new(tokens, outPath, rosPackageName, "srv", BuiltInTypesMapping, BuiltInTypesDefaultInitialValues, className);
            parser.Parse();
            warnings.AddRange(parser.GetWarnings());
        }
        return warnings;
    }
}
