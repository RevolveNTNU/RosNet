namespace RosNet.MessageGeneration;

public class ServiceCodeGenerator : CodeGenerator
{
    private static readonly string[] Types = { "Request", "Response" };
    public static new string FileExtension => ".srv";
    public static new string FileName => "service";

    protected override List<string> GenerateSingle(string inPath, string outPath, string? rosPackageName = "", bool? verbose = false)
    {
        // If no ROS package name is provided, extract from path
        rosPackageName ??= inPath.Split(Path.PathSeparator)[^3];
        outPath = Path.Combine(outPath, Utilities.ResolvePackageName(rosPackageName));

        string inFileName = Path.GetFileNameWithoutExtension(inPath);

        if (verbose ?? false)
        {
            Console.WriteLine("Parsing: " + inPath);
            Console.WriteLine("Output Location: " + outPath);
        }

        var tokenizer = new MessageTokenizer(inPath, new HashSet<string>(Utilities.BuiltInTypesMapping.Keys));
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

            MessageParser parser = new(tokens, outPath, rosPackageName, "srv", Utilities.BuiltInTypesMapping, Utilities.BuiltInTypesDefaultInitialValues, className);
            parser.Parse();
            warnings.AddRange(parser.GetWarnings());
        }
        return warnings;
    }
}