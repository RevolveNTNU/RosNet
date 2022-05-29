/*using System.Text;

using Microsoft.Extensions.Logging;

using static RosNet.MessageGeneration.Utilities;

namespace RosNet.MessageGeneration;

public class ActionCodeGenerator : CodeGenerator
{
    private static readonly string[] Types = { "Goal", "Result", "Feedback" };
    /// <inheritdoc cref="CodeGenerator.FileExtension"/>
    public static new string FileExtension => "action";
    /// <inheritdoc cref="CodeGenerator.FileName"/>
    public static new string FileName => "action";

    internal ActionCodeGenerator(ILogger<ActionCodeGenerator> logger) : base(logger)
    {
    }

    protected override IEnumerable<string> GenerateSingle(string inputPath, string? outputPath, string? rosPackageName)
    {
        // If no ROS package name is provided, extract from path
        rosPackageName ??= inputPath.Split(Path.DirectorySeparatorChar)[^3];

        outputPath = Path.Combine(outputPath ?? "", ResolvePackageName(rosPackageName));

        var inFileName = Path.GetFileNameWithoutExtension(inputPath);

        Logger.LogDebug("Parsing: {File}", inputPath);
        Logger.LogDebug("Output Location: {File}", outputPath);


        using var tokenizer = new MessageTokenizer(inputPath, new HashSet<string>(BuiltInTypesMapping.Keys));
        var listsOfTokens = tokenizer.Tokenize();

        if (listsOfTokens.Count != 3)
        {
            throw new MessageParserException("Unexpected number of sections. Action should have 3 sections.");
        }

        var warnings = new List<string>();
        var actionWrapper = new ActionWrapper(inputPath, rosPackageName, outputPath);

        for (int i = 0; i < listsOfTokens.Count; i++)
        {
            List<MessageToken> tokens = listsOfTokens[i];

            // Action is made up of goal, result, feedback
            string className = inFileName + Types[i];

            // Parse and generate goal, result, feedback messages
            var parser = new MessageParser(tokens, outputPath, rosPackageName, FileExtension, Utilities.BuiltInTypesMapping, Utilities.BuiltInTypesDefaultInitialValues, className, className);
            parser.Parse();
            warnings.AddRange(parser.Warnings);

            // Generate action section wrapper messages
            actionWrapper.WrapActionSections(Types[i]);
        }

        // Generate action wrapper
        actionWrapper.WrapAction();

        return warnings;
    }
}

internal class ActionWrapper
{
    private readonly string _inPath;
    private string InFileName => Path.GetFileNameWithoutExtension(_inPath);

    private readonly string _rosPackageName;

    private readonly string _outPath;

    private Dictionary<string, string> _symbolTable = new();

    public ActionWrapper(string inPath, string rosPackageName, string outPath)
    {
        this._inPath = inPath;
        this._rosPackageName = rosPackageName;
        this._outPath = Path.Combine(outPath, "action");
    }

    private string GenerateDefaultValueConstructor(string className)
    {
        string constructor = "";

        constructor += $"{TwoTabs}public {className}() : base()\n";
        constructor += TwoTabs + "{\n";

        foreach (string identifier in this._symbolTable.Keys)
        {
            constructor += $"{TwoTabs}{OneTab}this.{identifier} = ";
            string type = _symbolTable[identifier];
            constructor += $"new {type}();\n";
        }

        constructor += TwoTabs + "}\n";

        return constructor;
    }

    private string GenerateParameterizedConstructor(string className, string msgType)
    {
        var constructor = new StringBuilder();
        var paramsIn = new StringBuilder();
        var paramsOut = new StringBuilder();
        var assignments = new StringBuilder();

        switch (msgType)
        {
            case "Goal":
                paramsIn.Append("Header header, GoalID goal_id, ");
                paramsOut.Append("header, goal_id");
                break;
            case "Result" or "Feedback":
                paramsIn.Append("Header header, GoalStatus status, ");
                paramsOut.Append("header, status");
                break;
        }

        foreach (string identifier in this._symbolTable.Keys)
        {
            string type = _symbolTable[identifier];
            paramsIn.Append($"{type} {identifier}, ");
            assignments.AppendLine($"{TwoTabs}{OneTab}this.{identifier} = {identifier};");
        }

        var paramsInStr = paramsIn.Length != 0 ? paramsIn.ToString()[..^2] : paramsIn.ToString();

        constructor.AppendLine($"{TwoTabs}public {className}({paramsInStr}) : base({paramsOut})");
        constructor.AppendLine(TwoTabs + "{");
        constructor.Append(assignments);
        constructor.AppendLine(TwoTabs + "}");

        return constructor.ToString();
    }

    internal void WrapActionSections(string type)
    {
        string wrapperName = $"{InFileName}Action{type}";
        string msgName = $"{InFileName}{type}";

        string outPath = Path.Combine(this._outPath, $"{wrapperName}.cs");

        const string imports = "using RosNet.MessageTypes.Std;\n" +
                               "using RosNet.MessageTypes.Actionlib;\n\n";

        _symbolTable = new Dictionary<string, string>();

        using StreamWriter writer = new(outPath, false);
        // Write block comment
        writer.WriteLine(BlockComment);

        // Write imports
        writer.Write(imports);

        // Write namespace
        writer.Write($"namespace RosNet.MessageTypes.{ResolvePackageName(_rosPackageName)}\n{{\n");

        // Write class declaration
        writer.Write($"{OneTab}public class {wrapperName} : Action{type}<{InFileName}{type}>\n{OneTab}{{\n");

        // Write ROS package name
        writer.WriteLine($"{TwoTabs}public const string RosMessageName = \"{_rosPackageName}/{wrapperName}\";");

        // Record goal/result/feedback declaration
        _symbolTable.Add(char.ToLower(type[0]) + type[1..], msgName);

        writer.WriteLine("");

        // Write default value constructor
        writer.WriteLine(GenerateDefaultValueConstructor(wrapperName));

        // Write parameterized constructor
        writer.Write(GenerateParameterizedConstructor(wrapperName, type));

        // Close class
        writer.WriteLine(OneTab + "}");
        // Close namespace
        writer.WriteLine("}");
    }

    internal void WrapAction()
    {
        string wrapperName = InFileName + "Action";

        string outPath = Path.Combine(this._outPath, wrapperName + ".cs");

        _symbolTable = new Dictionary<string, string>();

        using StreamWriter writer = new(outPath, false);
        // Write block comment
        writer.WriteLine(BlockComment);

        // Write imports
        writer.Write("\n\n");

        // Write namespace
        writer.Write($"namespace RosNet.MessageTypes.{ResolvePackageName(_rosPackageName)}\n{{\n");

        // Write class declaration
        var genericParams = new[] {
                    InFileName + "ActionGoal",
                    InFileName + "ActionResult",
                    InFileName + "ActionFeedback",
                    InFileName + "Goal",
                    InFileName + "Result",
                    InFileName + "Feedback"
                };
        writer.WriteLine(
            $"{OneTab}public class {wrapperName} : Action<{string.Join(", ", genericParams)}>\n{OneTab}{{"
            );

        // Write ROS package name
        writer.WriteLine(
            $"{TwoTabs}public const string RosMessageName = \"{_rosPackageName}/{wrapperName}\";"
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
        writer.WriteLine(OneTab + "}");
        // Close namespace
        writer.WriteLine("}");
    }

}
*/
