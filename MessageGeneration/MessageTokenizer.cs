using Sprache;

namespace RosNet.MessageGeneration;

using RosFiles = IEnumerable<IEnumerable<ICommented<FieldDeclaration>>>;

/// A node in the Message file, can be a line, type, separator, identifier, etc.
public record Node(int Length, Position? StartPos) : IPositionAware<Node>
{
    /// <inheritdoc cref="IPositionAware{T}" />
    public Node SetPos(Position? startPos, int length) => this with { Length = length, StartPos = startPos };
}

/// <summary>
/// The full name of a ROS message type
/// </summary>
public record Type(string Name, string? Package = null, int Length = 0, Position? StartPos = null) : Node(Length, StartPos), IPositionAware<Type>
{
    /// <summary>
    /// The full name of a ROS message type
    /// </summary>
    /// <example>std_msgs/Header</example>
    public string FullName => $"{(Package != null ? $"{Package}/" : "")}{Name}";

    /// <inheritdoc/>
    public new Type SetPos(Position startPos, int length) => (Type)base.SetPos(startPos, length);
}

internal record ArrayType(string Name, string? Package, uint? ArrayLength, int Length = 0, Position? StartPos = null) : Type(Name, Package, Length, StartPos), IPositionAware<ArrayType>
{
    public new ArrayType SetPos(Position startPos, int length) => (ArrayType)base.SetPos(startPos, length);
}

/// <summary>
/// The name of a field in a ROS message
/// </summary>
/// <example>header</example>
/// <example>Foo</example>
public record Identifier(string Name, int Length = 0, Position? StartPos = null) : Node(Length, StartPos), IPositionAware<Identifier>
{
    /// <inheritdoc/>
    public new Identifier SetPos(Position startPos, int length) => (Identifier)base.SetPos(startPos, length);
}

/// <summary>
/// A full field declaration 
/// </summary>
/// <example>uint32 seq</example>
/// <example>Header header</example>
public record FieldDeclaration(Type Type, Identifier Identifier, int Length = 0, Position? StartPos = null) : Node(Length, StartPos), IPositionAware<FieldDeclaration>
{
    /// <inheritdoc/>
    public new FieldDeclaration SetPos(Position startPos, int length) => (FieldDeclaration)base.SetPos(startPos, length);
}

/// <summary>
/// A full field declaration 
/// </summary>
/// <example>int32 X=123</example>
/// <example>int32 Y=-123</example>
/// <example>string FOO=foo</example>
/// <example>string EXAMPLE="#comments" are ignored, and leading and trailing whitespace removed</example>
/// <remarks>
///     Unlike the other tokens (which are usually wrapped in a <see>IComment{T}</see>), the <c>Value</c> here contains the comment! As spec makes it kinda wonky to split now.
/// </remarks>
internal record ConstantDeclaration(Type Type, Identifier Identifier, string Value, int Length = 0, Position? StartPos = null) : FieldDeclaration(Type, Identifier, Length, StartPos), IPositionAware<ConstantDeclaration>
{
    public new ConstantDeclaration SetPos(Position startPos, int length) => (ConstantDeclaration)base.SetPos(startPos, length);
}

internal static class MessageTokenizer
{
    private static Parser<RosFiles> RosParser()
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

    internal static readonly Parser<RosFiles> RosMessageParser = RosParser();

    /// <summary>
    /// Tokenizes the stream input
    /// </summary>
    /// <returns> A list of Nodes read from the stream </returns>
    public static RosFiles Tokenize(in string file)
    {
        try
        {
            return RosMessageParser.Parse(file);
        }
        catch (ParseException e)
        {
            throw new MessageParserException("Failed to parse input", e);;
        }
    }
}
