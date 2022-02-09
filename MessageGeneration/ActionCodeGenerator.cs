using System.Text;

namespace RosNet.MessageGeneration;

public class ActionCodeGenerator : CodeGenerator
{
    private static readonly string[] Types = { "Goal", "Result", "Feedback" };
    public static new readonly string FileExtension = ".action";
    public static new readonly string FileName = "action";

    protected override List<string> GenerateSingle(string inPath, string outPath, string? rosPackageName, bool? verbose = false)
    {
        verbose ??= false;
        // If no ROS package name is provided, extract from path
        rosPackageName ??= inPath.Split(Path.PathSeparator)[^3];

        outPath = Path.Combine(outPath, Utilities.ResolvePackageName(rosPackageName));

        var inFileName = Path.GetFileNameWithoutExtension(inPath);

        if ((bool)verbose)
        {
            Console.WriteLine("Parsing: " + inPath);
            Console.WriteLine("Output Location: " + outPath);
        }

        using var tokenizer = new MessageTokenizer(inPath, new HashSet<string>(Utilities.BuiltInTypesMapping.Keys));
        var listsOfTokens = tokenizer.Tokenize();

        if (listsOfTokens.Count != 3)
        {
            throw new MessageParserException("Unexpected number of sections. Action should have 3 sections.");
        }

        var warnings = new List<string>();
        var actionWrapper = new ActionWrapper(inPath, rosPackageName, outPath);

        for (int i = 0; i < listsOfTokens.Count; i++)
        {
            List<MessageToken> tokens = listsOfTokens[i];

            // Action is made up of goal, result, feedback
            string className = inFileName + Types[i];

            // Parse and generate goal, result, feedback messages
            var parser = new MessageParser(tokens, outPath, rosPackageName, FileExtension, Utilities.BuiltInTypesMapping, Utilities.BuiltInTypesDefaultInitialValues, className, className);
            parser.Parse();
            warnings.AddRange(parser.GetWarnings());

            // Generate action section wrapper messages
            actionWrapper.WrapActionSections(Types[i]);
        }

        // Generate action wrapper
        actionWrapper.WrapAction();

        return warnings;
    }
}

public class ActionWrapper
{

    private const string ONE_TAB = "    ";
    private const string TWO_TABS = "        ";

    private readonly string _inPath;
    private readonly string _inFileName;

    private readonly string _rosPackageName;

    private readonly string _outPath;

    private Dictionary<string, string> _symbolTable = new();

    public ActionWrapper(string inPath, string rosPackageName, string outPath)
    {
        this._inPath = inPath;
        this._inFileName = Path.GetFileNameWithoutExtension(inPath);
        this._rosPackageName = rosPackageName;
        this._outPath = Path.Combine(outPath, "action");
    }

    private string GenerateDefaultValueConstructor(string className)
    {
        string constructor = "";

        constructor += $"{TWO_TABS}public {className}() : base()\n";
        constructor += TWO_TABS + "{\n";

        foreach (string identifier in this._symbolTable.Keys)
        {
            constructor += $"{TWO_TABS}{ONE_TAB}this.{identifier} = ";
            string type = _symbolTable[identifier];
            constructor += $"new {type}();\n";
        }

        constructor += TWO_TABS + "}\n";

        return constructor;
    }

    private string GenerateParameterizedConstructor(string className, string msgType)
    {
        var constructor = new StringBuilder();
        var paramsIn = new StringBuilder();
        var paramsOut = new StringBuilder();
        var assignments = new StringBuilder();

        if (msgType.Equals("Goal", StringComparison.Ordinal))
        {
            paramsIn.Append("Header header, GoalID goal_id, ");
            paramsOut.Append("header, goal_id");
        }
        else if (msgType.Equals("Result", StringComparison.Ordinal) || msgType.Equals("Feedback", StringComparison.Ordinal))
        {
            paramsIn.Append("Header header, GoalStatus status, ");
            paramsOut.Append("header, status");
        }

        foreach (string identifier in this._symbolTable.Keys)
        {
            string type = _symbolTable[identifier];
            paramsIn.Append($"{type} {identifier}, ");
            assignments.AppendLine($"{TWO_TABS}{ONE_TAB}this.{identifier} = {identifier};");
        }

        var paramsInStr = paramsIn.Length != 0 ? paramsIn.ToString()[0..^2] : paramsIn.ToString();

        constructor.AppendLine($"{TWO_TABS}public {className}({paramsInStr}) : base({paramsOut})");
        constructor.AppendLine(TWO_TABS + "{");
        constructor.Append(assignments);
        constructor.AppendLine(TWO_TABS + "}");

        return constructor.ToString();
    }

    public void WrapActionSections(string type)
    {
        string wrapperName = $"{_inFileName}Action{type}";
        string msgName = $"{_inFileName}{type}";

        string outPath = Path.Combine(this._outPath, $"{wrapperName}.cs");

        string imports =
            "using RosSharp.RosBridgeClient.MessageTypes.Std;\n" +
            "using RosSharp.RosBridgeClient.MessageTypes.Actionlib;\n\n";

        _symbolTable = new Dictionary<string, string>();

        using StreamWriter writer = new(outPath, false);
        // Write block comment
        writer.WriteLine(Utilities.BLOCK_COMMENT);

        // Write imports
        writer.Write(imports);

        // Write namespace
        writer.Write($"namespace RosSharp.RosBridgeClient.MessageTypes.{Utilities.ResolvePackageName(_rosPackageName)}\n{{\n");

        // Write class declaration
        writer.Write($"{ONE_TAB}public class {wrapperName} : Action{type}<{_inFileName}{type}>\n{ONE_TAB}{{\n");

        // Write ROS package name
        writer.WriteLine($"{TWO_TABS}public const string RosMessageName = \"{_rosPackageName}/{wrapperName}\";");

        // Record goal/result/feedback declaration
        _symbolTable.Add(Utilities.LowerFirstLetter(type), msgName);

        writer.WriteLine("");

        // Write default value constructor
        writer.WriteLine(GenerateDefaultValueConstructor(wrapperName));

        // Write parameterized constructor
        writer.Write(GenerateParameterizedConstructor(wrapperName, type));

        // Close class
        writer.WriteLine(ONE_TAB + "}");
        // Close namespace
        writer.WriteLine("}");
    }

    public void WrapAction()
    {
        string wrapperName = _inFileName + "Action";

        string outPath = Path.Combine(this._outPath, wrapperName + ".cs");

        string imports = "\n\n";

        _symbolTable = new Dictionary<string, string>();

        using StreamWriter writer = new(outPath, false);
        // Write block comment
        writer.WriteLine(Utilities.BLOCK_COMMENT);

        // Write imports
        writer.Write(imports);

        // Write namespace
        writer.Write($"namespace RosSharp.RosBridgeClient.MessageTypes.{Utilities.ResolvePackageName(_rosPackageName)}\n{{\n");

        // Write class declaration
        var genericParams = new string[] {
                    _inFileName + "ActionGoal",
                    _inFileName + "ActionResult",
                    _inFileName + "ActionFeedback",
                    _inFileName + "Goal",
                    _inFileName + "Result",
                    _inFileName + "Feedback"
                };
        writer.WriteLine(
            $"{ONE_TAB}public class {wrapperName} : Action<{string.Join(", ", genericParams)}>\n{ONE_TAB}{{"
            );

        // Write ROS package name
        writer.WriteLine(
            $"{TWO_TABS}public const string RosMessageName = \"{_rosPackageName}/{wrapperName}\";"
            );

        // Record variables
        // Action Goal
        _symbolTable.Add("action_goal", wrapperName + "Goal");
        // Action Result
        _symbolTable.Add("action_result", wrapperName + "Result");
        //Action Feedback
        _symbolTable.Add("action_feedback", wrapperName + "Feedback");

        // Write default value constructor
        writer.Write($"\n{GenerateDefaultValueConstructor(wrapperName)}\n");

        // Close class
        writer.WriteLine(ONE_TAB + "}");
        // Close namespace
        writer.WriteLine("}");
    }

}
