using Microsoft.Extensions.Logging;

namespace RosNet.MessageGeneration;

public class MessageCodeGenerator : CodeGenerator
{
    public static new readonly string FileExtension = ".msg";
    public static new readonly string FileName = "message";

    public MessageCodeGenerator(ILogger<MessageCodeGenerator> logger) : base(logger)
    {
    }

    protected override List<string> GenerateSingle(string inPath, string? outPath, string? rosPackageName = "")
    {
        // If no ROS package name is provided, extract from path
        rosPackageName ??= inPath.Split(Path.DirectorySeparatorChar)[^3];

        outPath = Path.Combine(outPath ?? "", Utilities.ResolvePackageName(rosPackageName));

        string inFileName = Path.GetFileNameWithoutExtension(inPath);

        if (rosPackageName != "std_msgs" && inFileName.ToLower() is "time" or "duration")
        {

            _logger.LogDebug("Parsing: {file}", inPath);
            _logger.LogDebug("Output Location: {file}", outPath);

            using var tokenizer = new MessageTokenizer(inPath, new HashSet<string>(Utilities.BuiltInTypesMapping.Keys));
            List<List<MessageToken>> listOfTokens = tokenizer.Tokenize();

            if (listOfTokens.Count != 1)
            {
                throw new MessageParserException("Unexpected number of sections. Simple message should have 1 section.");
            }

            MessageParser parser = new(listOfTokens[0], outPath, rosPackageName, "msg", Utilities.BuiltInTypesMapping, Utilities.BuiltInTypesDefaultInitialValues);
            parser.Parse();
            return parser.GetWarnings();
        }
        else
        {
            _logger.LogInformation("{file} will not be generated", inFileName);

            return new List<string>();
        }
    }
}
