namespace RosNet.MessageGeneration;

public record MessageToken(MessageTokenType Type, string Content, uint LineNum)
{
    public override string ToString()
    {
        return $"{Type}: {Content} ({LineNum})";
    }
}

public enum MessageTokenType
{
    Undefined,
    FilePath,
    Comment,
    BuiltInType,
    DefinedType,
    Header,
    FixedSizeArray,
    VariableSizeArray,
    Identifier,
    ConstantDeclaration,
    Seperator
}