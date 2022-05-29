using Microsoft.CodeAnalysis.CSharp;

using Sprache;

using static RosNet.MessageGeneration.Utilities;

namespace RosNet.MessageGeneration;


/// <summary>Parse ROS-message definitions</summary>
public static class MessageParser
{
    private static IEnumerable<string> ConstCapableTypes => BuiltInTypesMapping.Values.Where(t => t is not ("Time" or "Duration"));

    /// <summary>Assumes there is only one file in the file.</summary>
    internal static ParseResult ParseFile(in string file, in string rosMessageName, in string rosPackageName) => new(ParseFile(file).Single(), rosMessageName, rosPackageName);

    /// <summary>Parse a file into a list of sub-files with lists of fields and constants.</summary>
    public static IEnumerable<IEnumerable<Field>> ParseFile(in string file) => Tokenize(file).Select((tokens) => ParseFields(tokens));

    private static IEnumerable<Field> ParseFields(in IEnumerable<ICommented<FieldDeclaration>> tokens)
    {
        var declaredFields = new HashSet<string>();
        var isConst = (ICommented<FieldDeclaration> t) => t.Value is ConstantDeclaration;

        var initialConsts = tokens.TakeWhile(isConst).Select((token) => ParseField(token, declaredFields));
        var rest = tokens.SkipWhile(isConst);

        if (!rest.Any()) 
        {
            return initialConsts;
        }

        // Enumerable.Repeat(first, 1).Concat(tokens.Skip(1).Select((token) => ParseField(token, declaredFields)));
        // If the first _field_ (not const!) of your .msg is `Header header`, it resolves to std_msgs/Header
        // from https://wiki.ros.org/msg
        var firstField = ParseField(rest.First(), declaredFields);
        if (firstField is { Type: "Header", Name: "header", Package: null })
        {
            firstField = firstField with { Package = "std_msgs" };
        }

        return initialConsts.Concat(Enumerable.Repeat(firstField, 1)).Concat(rest.Skip(1).Select((token) => ParseField(token, declaredFields)));
    
    }

    private static Field ParseField(in ICommented<FieldDeclaration> token, in HashSet<string> declaredFieldNames)
    {
        var lc = token.LeadingComments;
        string? tc = token.TrailingComments.SingleOrDefault();
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

    private static Constant GenerateConstField(in Identifier identifier, in Type type, in string declaration)
    {
        var content = declaration.Split('#', 2);
        var val = content.First().Trim();
        var trailingComment = content.Skip(1);
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
        return new Constant(identifier.Name, type.Name, constDecl, Enumerable.Empty<string>(), type.Name is "string" ? null : trailingComment.SingleOrDefault());
    }

    private static Parser<IEnumerable<IEnumerable<ICommented<FieldDeclaration>>>> RosParser()
    {
        var commentParser = new CommentParser { Single = "#", NewLine = Environment.NewLine, MultiOpen = null, MultiClose = null };
        var nonNewLineWhiteSpaceParser = Parse.WhiteSpace.Except(Parse.LineTerminator);
        var rosIdentifierParser = Parse.Identifier(Parse.Letter, Parse.LetterOrDigit.Or(Parse.Char('_'))).Select(n => new Identifier(n)).Named("Identifier");
        var rosPackageParser = (from package in rosIdentifierParser
                                from slash in Parse.Char('/')
                                select package.Name).Named("PackageName");

        Parser<uint?> arrayLengthParser = Parse.Digit.XMany().Text().Contained(Parse.Char('['), Parse.Char(']')).Select(length => length.Length != 0 ? (uint?)uint.Parse(length) : null).Named("Array Declaration");

        var rosTypeParser = (from package in rosPackageParser.Optional()
                             from name in rosIdentifierParser
                             from arrayLength in arrayLengthParser.XOptional()
                             select arrayLength.IsEmpty ? new Type(name.Name, package.GetOrElse(null))
                                                         : new ArrayType(name.Name, package.GetOrElse(null), arrayLength.GetOrElse(null))).Named("Type");
        var constantDeclarationParser = (from eq in Parse.Char('=')
                                         from value in Parse.AnyChar.Until(Parse.LineTerminator).Text()
                                         select value).Named("Constant Declaration");
        var fieldParser = (from type in rosTypeParser.Positioned()
                           from ws in nonNewLineWhiteSpaceParser.AtLeastOnce()
                           from identifier in rosIdentifierParser.Positioned()
                           from c in constantDeclarationParser.XOptional()
                           select c.IsDefined
                           ? new ConstantDeclaration(type, identifier, c.Get())
                           : new FieldDeclaration(type, identifier)).Named("Field");
        return (from fields in fieldParser.Positioned().Commented(commentParser).Many()
                from _ in Parse.WhiteSpace.Many()
                select fields)
                .XDelimitedBy(Parse.String("---").Commented(commentParser).Named("Separator"))
                .End();
    }

    private static readonly Parser<IEnumerable<IEnumerable<ICommented<FieldDeclaration>>>> RosMessageParser = RosParser();

    /// <summary>
    /// Tokenizes the stream input
    /// </summary>
    /// <returns> A list of Nodes read from the stream </returns>
    private static IEnumerable<IEnumerable<ICommented<FieldDeclaration>>> Tokenize(in string file)
    {
        try
        {
            return RosMessageParser.Parse(file);
        }
        catch (ParseException e)
        {
            throw new MessageParserException("Failed to parse input", e); ;
        }
    }

    /// A node in the Message file, can be a line, type, separator, identifier, etc.
    private record Node(int Length, Position? StartPos) : IPositionAware<Node>
    {
        /// <inheritdoc cref="IPositionAware{T}" />
        public Node SetPos(Position? startPos, int length) => this with { Length = length, StartPos = startPos };
    }

    /// <summary>
    /// The full name of a ROS message type
    /// </summary>
    private record Type(string Name, string? Package = null, int Length = 0, Position? StartPos = null) : Node(Length, StartPos), IPositionAware<Type>
    {
        /// <summary>
        /// The full name of a ROS message type
        /// </summary>
        /// <example>std_msgs/Header</example>
        public string FullName => $"{(Package != null ? $"{Package}/" : "")}{Name}";

        /// <inheritdoc/>
        public new Type SetPos(Position startPos, int length) => (Type)base.SetPos(startPos, length);
    }

    private record ArrayType(string Name, string? Package, uint? ArrayLength, int Length = 0, Position? StartPos = null) : Type(Name, Package, Length, StartPos), IPositionAware<ArrayType>
    {
        public new ArrayType SetPos(Position startPos, int length) => (ArrayType)base.SetPos(startPos, length);
    }

    /// <summary>
    /// The name of a field in a ROS message
    /// </summary>
    /// <example>header</example>
    /// <example>Foo</example>
    private record Identifier(string Name, int Length = 0, Position? StartPos = null) : Node(Length, StartPos), IPositionAware<Identifier>
    {
        /// <inheritdoc/>
        public new Identifier SetPos(Position startPos, int length) => (Identifier)base.SetPos(startPos, length);
    }

    /// <summary>
    /// A full field declaration 
    /// </summary>
    /// <example>uint32 seq</example>
    /// <example>Header header</example>
    private record FieldDeclaration(Type Type, Identifier Identifier, int Length = 0, Position? StartPos = null) : Node(Length, StartPos), IPositionAware<FieldDeclaration>
    {
        /// <inheritdoc/>
        public new FieldDeclaration SetPos(Position startPos, int length) => (FieldDeclaration)base.SetPos(startPos, length);
    }

    /// <summary>
    /// A constant declaration 
    /// </summary>
    /// <example>int32 X=123</example>
    /// <example>int32 Y=-123</example>
    /// <example>string FOO=foo</example>
    /// <example>string EXAMPLE="#comments" are ignored, and leading and trailing whitespace removed</example>
    /// <remarks>
    ///     Unlike the other tokens (which are usually wrapped in a <see>IComment{T}</see>), the <c>Value</c> here contains the comment! As spec makes it kinda wonky to split now.
    /// </remarks>
    private record ConstantDeclaration(Type Type, Identifier Identifier, string Value, int Length = 0, Position? StartPos = null) : FieldDeclaration(Type, Identifier, Length, StartPos), IPositionAware<ConstantDeclaration>
    {
        public new ConstantDeclaration SetPos(Position startPos, int length) => (ConstantDeclaration)base.SetPos(startPos, length);
    }
}


internal record ParseResult(IEnumerable<Field> Fields, string RosMessageName, string RosPackageName);

/// <summary>
/// A full field declaration 
/// </summary>
/// <example>uint32 seq</example>
/// <example>Header header</example>
public record Field(
    string Name,
    string Type,
    IEnumerable<string> LeadingComments,
    string? TrailingComment=null,
    string? Package = null);

/// <summary>
/// A constant declaration 
/// </summary>
/// <example>int32 X=123</example>
/// <example>int32 Y=-123</example>
/// <example>string FOO=foo</example>
/// <example>string EXAMPLE="#comments" are ignored, and leading and trailing whitespace removed</example>
/// <remarks>
///     Unlike the other tokens (which are usually wrapped in a <see>IComment{T}</see>), the <c>Value</c> here contains the comment! As spec makes it kinda wonky to split now.
/// </remarks>
public record Constant(
    string Name,
    string Type,
    string Value,
    IEnumerable<string> LeadingComments,
    string? TrailingComment=null) : Field(Name, Type, LeadingComments, TrailingComment, null);

/// <summary>
/// An arrayfield declaration.
/// </summary>
/// <example>uint32[3] seq</example>
/// <example>string[] logs</example>
public record ArrayField(
    string Name,
    string Type,
    IEnumerable<string> LeadingComments,
    string? TrailingComment=null,
    string? Package = null,
    uint? ArraySize = null) : Field(Name, Type, LeadingComments, TrailingComment, Package)
{
    /// <summary>The type defintion of this field</summary>
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
    public Position? Position { get => _position ?? (InnerException as ParseException)?.Position; private set => _position = value; }

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
