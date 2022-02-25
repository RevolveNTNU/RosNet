using Microsoft.CodeAnalysis.CSharp;

using Sprache;

using static RosNet.MessageGeneration.Utilities;

namespace RosNet.MessageGeneration;

internal class MessageParser
{
    static IEnumerable<string> ConstCapableTypes => BuiltInTypesMapping.Values.Where(t => t is not ("Time" or "Duration"));

    internal static ParseResult Parse(in IEnumerable<ICommented<FieldDeclaration>> tokens, in string rosMessageName, in string rosPackageName) => new(Parse(tokens), rosMessageName, rosPackageName);

    internal static IEnumerable<IEnumerable<Field>> Parse(in string file) => MessageTokenizer.Tokenize(file).Select((tokens) => Parse(tokens));
    internal static IEnumerable<Field> Parse(in IEnumerable<ICommented<FieldDeclaration>> tokens)
    {
        if (!tokens.Any())
        {
            return Enumerable.Empty<Field>();
        }
        // If the first field of your .msg is `Header header`, it resolves to std_msgs/Header
        // from https://wiki.ros.org/msg
        var first = ParseField(tokens.First(), new());
        if (first is { Type: "Header", Name: "header", Package: null })
        {
            first = first with { Package = "std_msgs" };
        }

        var declaredFields = new HashSet<string>() { first.Name };
        return Enumerable.Repeat(first, 1).Concat(tokens.Skip(1).Select((token) => ParseField(token, declaredFields)));
    }

    private static Field ParseField(in ICommented<FieldDeclaration> token, in HashSet<string> declaredFieldNames)
    {
        var lc = token.LeadingComments;
        var tc = token.TrailingComments;
        var t = token.Value.Type;
        t = BuiltInTypesMapping.TryGetValue(t.Name, out var csharpType) && t.Package is null or "std_msgs" ? t with { Name = csharpType } : t;
        var i = token.Value.Identifier;
        return token.Value switch
        {
            // TODO: this should perhaps only be a warning?
            FieldDeclaration when !SyntaxFacts.IsValidIdentifier(i.Name) => throw new MessageParserException($"Invalid field identifier '{i.Name}'. '{i.Name}' is an invalid C# identifier", i.StartPos),
            FieldDeclaration when BuiltInTypesMapping.ContainsKey(i.Name) => throw new MessageParserException($"Invalid field identifier '{i.Name}'. '{i.Name}' is a ROS message built-in type.", i.StartPos),
            ConstantDeclaration when !ConstCapableTypes.Contains(t.FullName) => throw new MessageParserException($"Type {t.FullName}' cannot have constant declaration", token.Value.StartPos),
            // Note: this line adds the name to _declaredFieldNames, a little hidden state!
            FieldDeclaration when !declaredFieldNames.Add(i.Name) => throw new MessageParserException($"Field '{i.Name}' already declared!", i.StartPos),
            ConstantDeclaration { Value: var v } => GenerateConstField(i, t, v),
            FieldDeclaration { Type: ArrayType at } => new ArrayField(i.Name, t.Name, lc, tc, t.Package, at.ArrayLength),
            FieldDeclaration => new Field(i.Name, t.Name, lc, tc, t.Package),
        };
    }

    private static ConstField GenerateConstField(in Identifier identifier, in Type type, in string declaration)
    {
        var content = declaration.Split('#', 2);
        var val = content.First().Trim();
        var trailingComments = content.Skip(1);
        var constDecl = type.Name switch
        {
            "string" => $"\"{declaration.Trim()}\"",
            "bool" when (bool.TryParse(val, out bool b) && b) || (byte.TryParse(val, out byte a) && a != 0) => "true",
            "bool" when (bool.TryParse(val, out bool b) && !b) || (byte.TryParse(val, out byte a) && a == 0) => "false",
            "sbyte" when sbyte.TryParse(val, out _) => val,
            "byte" when byte.TryParse(val, out _) => val,
            "short" when short.TryParse(val, out _) => val,
            "ushort" when ushort.TryParse(val, out _) => val,
            "int" when int.TryParse(val, out _) => val,
            "uint" when uint.TryParse(val, out _) => val,
            "long" when long.TryParse(val, out _) => val,
            "ulong" when ulong.TryParse(val, out _) => val,
            "float" when float.TryParse(val, out _) => val,
            "double" when double.TryParse(val, out _) => val,
            _ => throw new MessageParserException($"Type mismatch: Expected {type.Name}, but value '{val}' is not {type.Name}.", type.StartPos),
        };
        return new ConstField(identifier.Name, type.Name, constDecl, Enumerable.Empty<string>(), trailingComments);
    }
}

internal record ParseResult(IEnumerable<Field> Fields, string RosMessageName, string RosPackageName);

internal record Field(
    string Name,
    string Type,
    IEnumerable<string> LeadingComments,
    IEnumerable<string> TrailingComments,
    string? Package = null);

internal record ConstField(
    string Name,
    string Type,
    string ConstantDeclaration,
    IEnumerable<string> LeadingComments,
    IEnumerable<string> TrailingComments) : Field(Name, Type, LeadingComments, TrailingComments, null);

internal record ArrayField(
    string Name,
    string Type,
    IEnumerable<string> LeadingComments,
    IEnumerable<string> TrailingComments,
    string? Package = null,
    uint? ArraySize = null) : Field(Name, Type, LeadingComments, TrailingComments, Package)
{
    public new string Type => base.Type + "[]";
}

/// <summary>
/// Failed to parse the ROS-message
/// </summary>
public class MessageParserException : Exception
{
    /// <summary>Path to file which was the source of the parsing exception</summary>
    public string? FilePath { get; }

    private Position? _position;

    /// <summary>The Position of the error in the file itself</summary>
    public Position? Position { get => _position ?? (InnerException as ParseException)?.Position; set => _position = value; }

    /// <inheritdoc />
    public MessageParserException(string message, Position? pos = null, string? filePath = null) : base(message)
    {
        FilePath = filePath;
        Position = pos;
    }

    /// <inheritdoc />
    public MessageParserException(string message, Exception inner) : base(message, inner) { }

    /// <inheritdoc />
    public override string ToString() => $"{base.ToString()} ({Position?.Line},{Position?.Column})";
}
