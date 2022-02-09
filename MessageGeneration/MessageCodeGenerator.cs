namespace RosNet.MessageGeneration;

public class MessageCodeGenerator : CodeGenerator
{
    public static new readonly string FileExtension = ".msg";
    public static new readonly string FileName = "message";
    protected override List<string> GenerateSingle(string inPath, string outPath, string? rosPackageName = "", bool? verbose = false)
    {
        // If no ROS package name is provided, extract from path
        rosPackageName ??= inPath.Split(Path.PathSeparator)[^3];

        outPath = Path.Combine(outPath, Utilities.ResolvePackageName(rosPackageName));

        string inFileName = Path.GetFileNameWithoutExtension(inPath);

        if (!(rosPackageName.Equals("std_msgs", StringComparison.Ordinal)
            && (inFileName.Equals("Time", StringComparison.Ordinal)
            || inFileName.Equals("Duration", StringComparison.Ordinal))))
        {
            if (verbose ?? false)
            {
                Console.WriteLine("Parsing: " + inPath);
                Console.WriteLine("Output Location: " + outPath);
            }

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
            if (verbose ?? false)
            {
                Console.WriteLine(inFileName + " will not be generated");
            }
            return new List<string>();
        }
    }
}
