using System.ComponentModel.DataAnnotations;
using System.Reflection;


namespace RosNet.MessageGeneration;

public abstract class CodeGenerator
{
    public static string FileExtension => throw new InvalidOperationException("Accessing fileextension of abstract class");

    public static string FileName => throw new InvalidOperationException("Accessing filename of abstract class");
    protected abstract List<string> GenerateSingle(string inputPath, string outputPath, string? rosPackageName = "", bool? verbose = false);
    public List<string> Generate(string inputPath, string outputPath, string? rosPackageName, bool? verbose)
    {
        var fileName = (string)this.GetType().GetProperty("FileName", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)!;
        var fileExtension = (string)this.GetType().GetProperty("FileExtension", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)!;

        var warnings = new List<string>();
        if (Directory.Exists(inputPath))
        {
            // TODO: make this flexible depending on 
            string[] files = Directory.GetFiles(inputPath, fileExtension, SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                Console.Error.WriteLine($"No {fileName} files found!");
                return warnings;
            }
            else
            {
                if (verbose ?? false)
                {
                    Console.WriteLine($"Found {files.Length} {fileName} files.");
                }
                foreach (string file in files)
                {
                    warnings.AddRange(GenerateSingle(file, outputPath, rosPackageName, verbose ?? false));
                }
            }
        }

        return warnings;
    }

}