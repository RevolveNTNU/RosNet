using Microsoft.CodeAnalysis.CSharp;

using static RosNet.MessageGeneration.Utilities;

namespace RosNet.MessageGeneration;

public class MessageParser
{

    private readonly List<MessageToken> _tokens;

    private readonly string _inFilePath;
    private readonly string _inFileName;

    private readonly string _rosPackageName;
    private readonly string _className;
    private readonly string _rosMsgName;

    private readonly string _outPath;
    private string _outFilePath;

    private readonly HashSet<string> _imports = new();

    private readonly Dictionary<string, string> _builtInTypeMapping;
    private readonly Dictionary<string, string> _builtInTypesDefaultInitialValues;

    private uint _lineNum = 1;

    private readonly Dictionary<string, string> _symbolTable = new();
    private readonly HashSet<string> _constants = new();
    private readonly Dictionary<string, int> _arraySizes = new();

    private string _body = "";

    private readonly List<string> _warnings = new();

    public MessageParser(List<MessageToken> tokens, string outPath, string rosPackageName, string type, Dictionary<string, string> builtInTypeMapping, Dictionary<string, string> builtInTypesDefaultInitialValues, string className = "", string rosMsgName = "")
    {
        this._tokens = tokens;

        this._inFilePath = tokens[0].Content;
        this._inFileName = Path.GetFileNameWithoutExtension(_inFilePath);

        this._rosPackageName = rosPackageName;

        this._className = className.Length == 0 ? CapitalizeFirstLetter(_inFileName) : className;

        this._rosMsgName = rosMsgName.Length == 0 ? CapitalizeFirstLetter(_inFileName) : rosMsgName;


        this._outPath = outPath;
        this._outFilePath = Path.Combine(outPath, type);

        this._tokens.RemoveAt(0);

        this._builtInTypeMapping = builtInTypeMapping;
        this._builtInTypesDefaultInitialValues = builtInTypesDefaultInitialValues;
    }

    public void Parse()
    {
        // If outpath doesn't exist, mkdir
        if (!Directory.Exists(_outFilePath))
        {
            Directory.CreateDirectory(_outFilePath);
        }
        // Append filename
        this._outFilePath = Path.Combine(this._outFilePath, this._className + ".cs");

        using StreamWriter writer = new(_outFilePath, false);
        writer.Write(Utilities.BLOCK_COMMENT + "\n");

        // Message -> Lines
        // Lines -> Line Lines | e
        while (_tokens.Count != 0)
        {
            Line();
        }

        // Write imports
        writer.Write(GenerateImports());

        // Write namespace
        writer.Write(
            "namespace RosSharp.RosBridgeClient.MessageTypes." + Utilities.ResolvePackageName(_rosPackageName) + "\n" +
            "{\n"
            );

        // Write class declaration
        writer.Write(
            $"{Utilities.ONE_TAB}public class {_className} : Message\n{Utilities.ONE_TAB}{{\n"
            );

        // Write ROS package name
        writer.Write($"{Utilities.TWO_TABS}public const string RosMessageName = \"{_rosPackageName}/{_rosMsgName}\";\n\n");

        // Write body
        writer.WriteLine(_body);

        // Write constructors
        writer.Write(GenerateDefaultValueConstructor());
        if (_symbolTable.Count != 0 && !new HashSet<string>(_symbolTable.Keys).SetEquals(_constants))
        {
            writer.Write("\n");
            writer.Write(GenerateParameterizedConstructor());
        }

        // Close class
        writer.WriteLine(Utilities.ONE_TAB + "}");
        // Close namespace
        writer.WriteLine("}");

        writer.Flush();
        writer.Close();
    }

    // Line -> Comment | Declaration
    private void Line()
    {
        MessageToken? peeked = this.Peek();
        if (peeked == null)
        {
            throw new MessageParserException("Unexpected end of input", _inFileName, _lineNum);
        }
        else if (peeked.Type == MessageTokenType.Comment)
        {
            Comment();
        }
        else if (peeked.Type is MessageTokenType.BuiltInType or MessageTokenType.DefinedType or MessageTokenType.Header)
        {
            Declaration();
        }
        else
        {
            throw new MessageParserException($"Unexpected token '{peeked.Content}'. Expecting a comment or field declaration.", _inFilePath, _lineNum);
        }
    }

    // Comment -> # sigma* \n
    private void Comment() => _body += $"{Utilities.TWO_TABS}// {MatchByType(MessageTokenType.Comment)}\n";

    // Declaration -> BuiltInType Identifier | BuiltInType Identifier ConstantDeclaration | BuiltInType ArrayDeclaration Identifier
    // Declaration -> DefinedType Identifier | DefinedType ArrayDeclaration Identifier
    // Declaration -> Header Identifier
    private void Declaration()
    {
        // Type
        var peeked = Peek() ?? throw new MessageParserException("Unexpected end of file.", _inFilePath, _lineNum);

        var declaration = $"{Utilities.TWO_TABS}public ";
        var canHaveConstDecl = false;
        string type;

        if (peeked.Type == MessageTokenType.BuiltInType)
        {
            type = _builtInTypeMapping[MatchByType(MessageTokenType.BuiltInType)];
            if (type.ToLower() is "time" or "duration")
            {
                // Need to import Std, as these types are defined by us, unlike most primitives
                _imports.Add("Std");
            }
            else
            {
                // Time and Duration can't have constant declaration
                // See <wiki.ros.org/msg>
                canHaveConstDecl = true;
            }
        }
        else if (peeked.Type == MessageTokenType.DefinedType)
        {
            type = MatchByType(MessageTokenType.DefinedType);
            string[] hierarchy = type.Split('/');
            // Assume type can only be either:
            // Type
            // package/Type
            switch (hierarchy.Length)
            {
                case 1:
                    break;
                case 2:
                    if (hierarchy[0].Length == 0 || hierarchy[1].Length == 0)
                    {
                        goto default;
                    }
                    string package = Utilities.ResolvePackageName(hierarchy[0]);
                    _imports.Add(package);
                    type = hierarchy[1];
                    break;
                default:
                    throw new MessageParserException($"Invalid field type '{type}'.", _inFilePath, _lineNum);
            }
        }
        else
        {
            type = MatchByType(MessageTokenType.Header);
            if (peeked.Type is MessageTokenType.FixedSizeArray or MessageTokenType.VariableSizeArray)
            {
                Warn($"By convention, there is only one header for each message. ({_inFilePath}:{_lineNum})");
            }
            if (peeked.Type == MessageTokenType.Identifier && peeked.Content != "header")
            {
                Warn($"By convention, a ros message Header will be named 'header'. '{peeked.Content}'. ({_inFilePath}:{_lineNum})");
            }
            _imports.Add("Std");
        }

        // Array Declaration
        int? arraySize = null;
        if (peeked.Type is MessageTokenType.FixedSizeArray or MessageTokenType.VariableSizeArray)
        {
            type += "[]";
            canHaveConstDecl = false;
            arraySize = peeked.Type == MessageTokenType.FixedSizeArray ? int.Parse(MatchByType(MessageTokenType.FixedSizeArray)) : 0;
            if (peeked.Type == MessageTokenType.VariableSizeArray)
            {
                MatchByType(MessageTokenType.VariableSizeArray);
            }
        }

        // Identifier
        string identifier = MatchByType(MessageTokenType.Identifier);
        // Check for duplicate declaration
        // Check if identifier is a ROS message built-in type
        if (_builtInTypeMapping.ContainsKey(identifier) || identifier.ToLower() is "time" or "duration")
        {
            throw new MessageParserException($"Invalid field identifier '{identifier}'. '{identifier}' is a ROS message built-in type.", _inFilePath, _lineNum);
        }

        // Check if identifier is a C# keyword
        if (!SyntaxFacts.IsValidIdentifier(identifier))
        {
            declaration = $"{Utilities.TWO_TABS}[JsonProperty(\"{identifier}\")]\n{declaration}";
            identifier = "_" + identifier;
            if (!SyntaxFacts.IsValidIdentifier(identifier))
            {
                throw new MessageParserException($"Invalid field identifier '{identifier}'. '{identifier} is an invalid C# identifier, even with a prepended \"_\".", _inFilePath, _lineNum);
            }
            Warn($"'{identifier}' is not a valid C# Identifier. We have prepended \"_\" to avoid C# compile-time issues. ({_inFilePath}:{_lineNum})");
        }

        if (!_symbolTable.TryAdd(identifier, type))
        {
            throw new MessageParserException($"Field '{identifier}' already declared!", _inFileName, _lineNum);
        }

        // Array declaration table
        if (arraySize is int s)
        {
            _arraySizes.Add(identifier, s);
        }

        // Constant Declaration
        if (peeked.Type == MessageTokenType.ConstantDeclaration)
        {
            if (!canHaveConstDecl)
            {
                throw new MessageParserException($"Type {type}' cannot have constant declaration", _inFilePath, _lineNum);
            }
            declaration += $"const {type} {identifier} = {ConstantDeclaration(type)}";
            _constants.Add(identifier);
        }
        else
        {
            declaration += $"{type} {identifier} {{ get; set; }}\n";
        }
        _body += declaration;
    }

    // Constant Declaration -> = NumericalConstantValue Comment
    // Constant Declaration -> = StringConstantValue
    // Note that a comment cannot be present in a string constant definition line
    private string ConstantDeclaration(string type)
    {
        var declaration = MatchByType(MessageTokenType.ConstantDeclaration);
        if (type == "string")
        {
            return $"\"{declaration.Trim()}\";\n";
        }
        var content = declaration.Split('#', 2);
        var val = content[0].Trim();
        var comment = content.Length > 1 ? $" // {content[1]}" : "";

        var ret = type switch
        {
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
            _ => throw new MessageParserException($"Type mismatch: Expected {type}, but value '{val}' is not {type}.", _inFilePath, _lineNum),
        };

        return $"{ret};{comment}\n";
    }

    private string GenerateImports()
    {
        string importsStr = "\n\n";
        if (_imports.Count > 0)
        {
            foreach (string s in _imports)
            {
                importsStr += $"using RosSharp.RosBridgeClient.MessageTypes.{s};\n";
            }
            importsStr += "\n";
        }
        return importsStr;
    }

    private string GenerateDefaultValueConstructor()
    {
        string constructor = $"{Utilities.TWO_TABS}public {_className}()\n";
        constructor += Utilities.TWO_TABS + "{\n";

        foreach (string identifier in _symbolTable.Keys)
        {
            if (!_constants.Contains(identifier))
            {
                constructor += $"{Utilities.TWO_TABS}{Utilities.ONE_TAB}this.{identifier} = ";
                string type = _symbolTable[identifier];
                if (_builtInTypesDefaultInitialValues.ContainsKey(type))
                {
                    constructor += _builtInTypesDefaultInitialValues[type];
                }
                else if (_arraySizes.ContainsKey(identifier))
                {
                    constructor += $"new {type[..^1]}{_arraySizes[identifier]}]";
                }
                else
                {
                    constructor += $"new {type}()";
                }
                constructor += ";\n";
            }
        }

        constructor += Utilities.TWO_TABS + "}\n";

        return constructor;
    }

    private string GenerateParameterizedConstructor()
    {
        string constructor = "";

        string parameters = "";
        string assignments = "";

        foreach (string identifier in _symbolTable.Keys)
        {
            if (!_constants.Contains(identifier))
            {
                string type = _symbolTable[identifier];
                parameters += $"{type} {identifier}, ";
                assignments += $"{Utilities.TWO_TABS}{Utilities.ONE_TAB}this.{identifier} = {identifier};\n";
            }
        }

        if (parameters.Length != 0)
        {
            parameters = parameters[0..^2];
        }

        constructor += $"{Utilities.TWO_TABS}public {_className}({parameters})\n";
        constructor += Utilities.TWO_TABS + "{\n";
        constructor += assignments;
        constructor += Utilities.TWO_TABS + "}\n";

        return constructor;
    }

    private string MatchByType(MessageTokenType type)
    {
        MessageToken token = _tokens[0];
        if (token.Type != type)
        {
            throw new MessageParserException(
                $"Unexpected token '{token.Content}'. Expected a token of type {Enum.GetName(typeof(MessageTokenType), token.Type)}", _inFilePath, token.LineNum);
        }

        _tokens.RemoveAt(0);
        // Update line num
        if (_tokens.Count != 0)
        {
            _lineNum = _tokens[0].LineNum;
        }
        return token.Content;

    }

    private MessageToken? Peek() => _tokens.Count == 0 ? null : _tokens[0];

    private void Warn(string msg) => _warnings.Add(msg);

    public List<string> GetWarnings() => _warnings;
}

public class MessageParserException : System.Exception
{
    public readonly uint? LineNum = null;
    public readonly string? FilePath;
    public override string? Source => FilePath ?? base.Source;
    public MessageParserException(string message, string? filePath = null, uint? lineNum = null) : base(message)
    {
        FilePath = filePath;
        LineNum = lineNum;
    }
    public MessageParserException(string message, System.Exception inner) : base(message, inner) { }

    public override string ToString() => $"{base.ToString()} ({FilePath}:{LineNum})";
}
