using Microsoft.Extensions.Logging;

using static RosNet.MessageGeneration.Utilities;

namespace RosNet.MessageGeneration;

/// <summary>Generate C# Code for ROS Messages</summary>
public abstract class CodeGenerator
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logger"><see cref="Logger"/></param>
    protected internal CodeGenerator(ILogger<CodeGenerator> logger)
    {
        Logger = logger;
    }

    /// <summary>
    /// The file extension for the type of file used as input for this generator.
    /// </summary>
    /// <example>msg</example>
    public static string FileExtension => throw new InvalidOperationException("Accessing fileextension of abstract class");

    /// <summary>
    /// Human-friendly name for the files this genrator reads.
    /// </summary>
    /// <example>Message</example>
    public static string FileName => throw new InvalidOperationException("Accessing filename of abstract class");

    private protected ILogger<CodeGenerator> Logger { get; }

    /// <summary>
    ///     Read in a file, and generate a C#-class equivalent of the ROS-file.
    /// </summary>
    /// <param name="inputPath">Path to *.{msg,srv,action} file</param>
    /// <param name="outputPath">Directory to store generated file in</param>
    /// <param name="rosPackageName">Optionally override the package name, affects namespace of generated code.</param>
    protected abstract void GenerateSingle(string inputPath, string? outputPath, string? rosPackageName);

    /// <summary>
    /// Read in a file (or directory), and generate a C#-class equivalent of the ROS-file.
    /// </summary>
    /// <param name="inputPath">either a single file, or the path to a ROS-message-folder, in the "usual" way, aka, {inputPath}/{msg,srv,action}/*.{msg/srv/action}.</param>
    /// <param name="outputPath">Directory to store generated file(s) in</param>
    /// <param name="rosPackageName">Optionally override the package name, affects namespace of output code.</param>
    public void Generate(string inputPath, string? outputPath, string? rosPackageName)
    {
        var fileName = (string)this.GetType().GetField("FileName")?.GetValue(null)!;
        var fileExtension = (string)this.GetType().GetField("FileExtension")?.GetValue(null)!;

        if (Directory.Exists(inputPath))
        {
            // TODO: make this flexible depending on 
            string[] files = Directory.GetFiles(inputPath + fileExtension, "*." + fileExtension, SearchOption.TopDirectoryOnly);

            if (files.Length == 0)
            {
                Logger.LogWarning("No files found in {FileName}!", fileName);
            }

            Logger.LogDebug("Found {Count} files in {Directory}.", files.Length, fileName);
            foreach (string file in files)
            {
                GenerateSingle(file, outputPath, rosPackageName);
            }
        }
        else
        {
            Logger.LogDebug("Generating code for {File}", inputPath);
            GenerateSingle(inputPath, outputPath, rosPackageName);
        }
    }

    internal static string GenerateCode(ParseResult msg)
    {
        string template = @"
{0}
{1}

namespace RosNet.MessageTypes.{2}
{
    public class {3} : Message
    {
        public const RosMessageName = ""{3}"";
{4}
{5}
{6}
    }
}";
        var declarations = msg.Fields.Aggregate(TwoTabs, (sum, f) => sum + ToDeclaration(f));
        return string.Format(template, BlockComment, Imports(msg.Fields), msg.RosPackageName, msg.RosMessageName, declarations, DefaultValueConstructor(msg), ParameterizedValueConstructor(msg));
    }

    // Missing from ROS# : Invalid C# indentifiers are allowed, so they add JsonProperty
    // TODO (MAJOR SECURITY-ISSUE): ROS-comments can freely contain /* */
    internal static string ToDeclaration(Field f)
    {
        var leading = f.LeadingComments.Any() ? $"        /* {f.LeadingComments} */\n" : "";
        var trailing = f.TrailingComments.Any() ? $"        /* {f.TrailingComments} */\n" : "\n";
        var decl = f switch
        {
            Constant { Type: var t, Name: var n, Value: var d } => $"        public const {t} {n} = {d};",
            Field { Type: var t, Name: var n } => $"        public {t} {n} {{ get; set; }}",
        };
        return leading + decl + trailing;
    }

    internal static string Imports(IEnumerable<Field> fields) => fields.Select(f => f.Package).Distinct().Aggregate("\n\n", (current, p) => current + p switch
    {
        null => "",
        "std_msgs" => $"using RosNet.MessageTypes.Std;\n",
        _ => $"using RosNet.MessageTypes.{p};\n",
    });

    internal static string DefaultValueConstructor(ParseResult message)
    {
        string template = @"
        public {0}()
        {
            {1}
        }
";

        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/default#default-literal
        var fieldInit = message.Fields.Where(f => f is not Constant).Aggregate("", (sum, f) =>
        {
            var initialization = f switch
            {
                ArrayField { ArraySize: var s, Type: var t } => $"new {t}[{s}]",
                _ => "default",
            };
            return sum + $"{TwoTabs}{OneTab}this.{f.Name} = {initialization};\n";
        })!;

        return string.Format(template, message.RosMessageName, fieldInit);
    }

    private static string ParameterizedValueConstructor(ParseResult message)
    {
        string template = @"
        public {0}({1})
        {
            {2}
        }
";
        var parameters = message.Fields.Where(f => f is not Constant).Aggregate("", (sum, f) => $"{f.Type} {f.Name}");
        var paramsString = string.Join(", ", parameters);
        var assignments = message.Fields.Where(f => f is not Constant).Aggregate("", (sum, f) => $"{TwoTabs}{OneTab}this.{f.Name} = {f.Name};");
        var fieldAssignmentString = string.Join('\n', assignments);

        return string.Format(template, message.RosMessageName, paramsString, fieldAssignmentString);
    }
}
