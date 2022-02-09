using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;

using Microsoft.Extensions.Logging;

using RosNet.MessageGeneration;

static void PrintWarnings(List<string> warnings)
{
    if (warnings.Count > 0)
    {
        Console.WriteLine($"You have {warnings.Count} warnings");
        foreach (string w in warnings)
        {
            Console.WriteLine(w);
        }
    }
}

var outputOpt = new Option<string>(new[] { "--output", "-o" }, "Specify output path\nIf unspecified, output will be in current working directory, under RosNetMessages").LegalFilePathsOnly();
var nameOpt = new Option<string>(new[] { "--ros-package-name", "-n" }, "Specify the ROS package name for the message\nIf unspecified, package name will be retrieved from path, assuming ROS package structure");
var verboseOpt = new Option<bool>(new[] { "--verbose", "-v" }, "Outputs extra information");
var inputArg = new Argument<string>("input-path", "The path to either a singel {message,service,action} file, or package").LegalFilePathsOnly();

var serviceCmd = new Command("service", "Generate service messages");
var actionCmd = new Command("action", "Generate action messages");

var description = @"Note:
- std_msgs/Time and std_msgs/Duration will not be generated since they need to be defined with primitive variables
- The Message abstract class will also not be generated, but is required since all generated message classes inherit it
Those can be found at the RosNet GitHub repo <https://github.com/revolventnu/rosnet>";

var rootCommand = new RootCommand(description) {
    serviceCmd,
    actionCmd,
};
rootCommand.AddArgument(inputArg);
rootCommand.AddGlobalOption(verboseOpt);
rootCommand.AddGlobalOption(outputOpt);
rootCommand.AddGlobalOption(nameOpt);

static void Handler<T>(string inputPath, string? outputPath, string? rosPackageName, bool verbose) where T : CodeGenerator
{
    using var loggerFactory = LoggerFactory.Create(builder => builder
                .AddFilter("Microsoft", LogLevel.Warning)
                .AddFilter("System", LogLevel.Warning)
                .AddFilter("MessageGeneration", verbose ? LogLevel.Debug : LogLevel.Information)
                .AddConsole());
    // C# Type system fighting hard
    var c = typeof(T).GetConstructor(new[] { typeof(ILogger<T>) })!;
    var generator = (T)c.Invoke(new[] { loggerFactory.CreateLogger<T>() });
    PrintWarnings(generator.Generate(inputPath, outputPath, rosPackageName));
};

actionCmd.SetHandler((string inputPath, string? outputPath, string? rosPackageName, bool verbose) => Handler<ActionCodeGenerator>(inputPath, outputPath, rosPackageName, verbose), inputArg, outputOpt, nameOpt, verboseOpt);
serviceCmd.SetHandler((string inputPath, string? outputPath, string? rosPackageName, bool verbose) => Handler<ServiceCodeGenerator>(inputPath, outputPath, rosPackageName, verbose), inputArg, outputOpt, nameOpt, verboseOpt);
rootCommand.SetHandler((string inputPath, string? outputPath, string? rosPackageName, bool verbose) => Handler<MessageCodeGenerator>(inputPath, outputPath, rosPackageName, verbose), inputArg, outputOpt, nameOpt, verboseOpt);

var cmdBuilder = new CommandLineBuilder(rootCommand);
cmdBuilder.UseDefaults();
var cmd = cmdBuilder.Build();

return cmd.Parse(args).Invoke();
