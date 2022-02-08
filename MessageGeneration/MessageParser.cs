using Microsoft.CodeAnalysis.CSharp;

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

        this._className = className.Length == 0 ? Utilities.CapitalizeFirstLetter(_inFileName) : className;

        this._rosMsgName = rosMsgName.Length == 0 ? Utilities.CapitalizeFirstLetter(_inFileName) : rosMsgName;


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
        while (!IsEmpty())
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
        if (PeekType(MessageTokenType.Comment))
        {
            Comment();
        }
        else if (PeekType(MessageTokenType.BuiltInType) || PeekType(MessageTokenType.DefinedType) || PeekType(MessageTokenType.Header))
        {
            Declaration();
        }
        else
        {
            // Mumble mumble
            if (peeked == null)
            {
                throw new MessageParserException(
                    $"Unexpected end of input ' at {_inFilePath}:{_lineNum}");
            }
            else
            {
                throw new MessageParserException(
                    $"Unexpected token '{peeked.Content}' at {_inFilePath}:{_lineNum}. Expecting a comment or field declaration.");
            }
        }
    }

    // Comment -> # sigma* \n
    private void Comment()
    {
        _body += $"{Utilities.TWO_TABS}// {MatchByType(MessageTokenType.Comment)}\n";
    }

    // Declaration -> BuiltInType Identifier | BuiltInType Identifier ConstantDeclaration | BuiltInType ArrayDeclaration Identifier
    // Declaration -> DefinedType Identifier | DefinedType ArrayDeclaration Identifier
    // Declaration -> Header Identifier
    private void Declaration()
    {
        string declaration = "";
        // Type
        MessageToken? peeked = Peek();
        bool canHaveConstDecl = false;
        declaration += Utilities.TWO_TABS + "public ";
        string type;
        if (PeekType(MessageTokenType.BuiltInType))
        {
            type = _builtInTypeMapping[MatchByType(MessageTokenType.BuiltInType)];
            if (!type.Equals("Time", StringComparison.Ordinal) && !type.Equals("Duration", StringComparison.Ordinal))
            {
                // Time and Duration can't have constant declaration
                // See <wiki.ros.org/msg>
                canHaveConstDecl = true;
            }
            else
            {
                // Need to import Standard
                _imports.Add("Std");
            }
        }
        else if (PeekType(MessageTokenType.DefinedType))
        {
            type = MatchByType(MessageTokenType.DefinedType);
            string[] hierarchy = type.Split(new char[] { '/', '\\' });
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
                        throw new MessageParserException(
                        $"Invalid field type '{type}'. + ({_inFilePath}:{_lineNum})");
                    }
                    string package = Utilities.ResolvePackageName(hierarchy[0]);
                    _imports.Add(package);
                    type = hierarchy[1];
                    break;
                default:
                    throw new MessageParserException($"Invalid field type '{type}'. + ({_inFilePath}:{_lineNum})");
            }
        }
        else
        {
            type = MatchByType(MessageTokenType.Header);
            if (PeekType(MessageTokenType.FixedSizeArray) || PeekType(MessageTokenType.VariableSizeArray))
            {
                Warn($"By convention, there is only one header for each message.({_inFilePath}:{_lineNum})");
            }
            // FIXME: can peeked be null?
            if (PeekType(MessageTokenType.Identifier) && !peeked!.Content.Equals("header", StringComparison.Ordinal))
            {
                this.Warn($"By convention, a ros message Header will be named 'header'. '{peeked.Content}'. ({_inFilePath}:{_lineNum})");
            }
            _imports.Add("Std");
        }

        // Array Declaration
        int arraySize = -1;
        if (PeekType(MessageTokenType.FixedSizeArray))
        {
            type += "[]";
            canHaveConstDecl = false;
            arraySize = int.Parse(MatchByType(MessageTokenType.FixedSizeArray));
        }
        if (PeekType(MessageTokenType.VariableSizeArray))
        {
            type += "[]";
            canHaveConstDecl = false;
            MatchByType(MessageTokenType.VariableSizeArray);
            arraySize = 0;
        }

        // Identifier
        string identifier = MatchByType(MessageTokenType.Identifier);
        // Check for duplicate declaration
        if (_symbolTable.ContainsKey(identifier))
        {
            throw new MessageParserException($"Field '{identifier}' at {_inFilePath}:{_lineNum} already declared!");
        }
        // Check if identifier is a ROS message built-in type
        if (_builtInTypeMapping.ContainsKey(identifier) && identifier.Equals("time", StringComparison.Ordinal) && identifier.Equals("duration", StringComparison.Ordinal))
        {
            throw new MessageParserException(
                $"Invalid field identifier '{identifier}' at {_inFilePath}:{_lineNum}. '{identifier}' is a ROS message built-in type.");
        }


        // Check if identifier is a C# keyword
        if (SyntaxFacts.IsValidIdentifier(identifier))
        {
            Warn($"'{identifier}' is not a valid C# Identifier. We have appended \"_\" at the front to avoid C# compile-time issues.({_inFilePath}:{_lineNum})");
            declaration = $"{Utilities.TWO_TABS}[JsonProperty(\"{identifier}\")]\n{declaration}";
            identifier = "_" + identifier;
        }

        _symbolTable.Add(identifier, type);

        // Array declaration table
        if (arraySize > -1)
        {
            _arraySizes.Add(identifier, arraySize);
        }

        // Constant Declaration
        if (PeekType(MessageTokenType.ConstantDeclaration))
        {
            if (canHaveConstDecl)
            {
                declaration += $"const {type} {identifier} = {ConstantDeclaration(type)}";
                _constants.Add(identifier);
            }
            else
            {
                throw new MessageParserException(
                    $"Type {type}' at {_inFilePath}:{_lineNum} cannot have constant declaration");
            }
        }
        else
        {
            declaration += $"{type} {identifier}{Utilities.PROPERTY_EXTENSION}\n";
        }
        _body += declaration;
    }

    // Constant Declaration -> = NumericalConstantValue Comment
    // Constant Declaration -> = StringConstantValue
    // Note that a comment cannot be present in a string constant definition line
    private string ConstantDeclaration(string type)
    {
        string declaration = MatchByType(MessageTokenType.ConstantDeclaration);
        if (type.Equals("string", StringComparison.Ordinal))
        {
            return "\"" + declaration.Trim() + "\";\n";
        }
        else
        {
            string ret = "";
            string comment = "";
            // Parse constant value using exisiting C# routines
            // Parse by invoking method
            // First check if a comment exists
            string val;
            if (declaration.Contains('#'))
            {
                string[] contents = declaration.Split('#');
                val = contents[0].Trim();
                comment = string.Join("#", contents, 1, contents.Length - 1);
            }
            else
            {
                val = declaration.Trim();
            }
            // Parse value
            ret += type switch
            {
                "bool" when val.Equals("True", StringComparison.Ordinal) || (byte.TryParse(val, out byte a) && a != 0) => "true",
                "bool" when val.Equals("False", StringComparison.Ordinal) || (byte.TryParse(val, out byte a) && a == 0) => "false",
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
                _ => throw new MessageParserException($"Type mismatch: Expecting {type}, but value '{val}' at {this._inFilePath}:{this._lineNum} is not {type}."),
            };
            ret += ";";

            // Take care of comment
            if (comment.Length != 0)
            {
                ret += $" // {comment}";
            }

            return $"{ret}\n";
        }
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
        string constructor = "";

        constructor += $"{Utilities.TWO_TABS}public {_className}()\n";
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
                    constructor += $"new {type.Remove(type.Length - 1)}{_arraySizes[identifier]}]";
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
        if (token.Type.Equals(type))
        {
            _tokens.RemoveAt(0);
            // Update line num
            if (!IsEmpty())
            {
                _lineNum = _tokens[0].LineNum;
            }
            return token.Content;
        }
        else
        {
            throw new MessageParserException(
                $"Unexpected token '{token.Content}' at {_inFilePath}:{token.LineNum}. Expecting a token of type {Enum.GetName(typeof(MessageTokenType), token.Type)}");
        }
    }

    private MessageToken? Peek() => IsEmpty() ? null : _tokens[0];

    private bool PeekType(MessageTokenType type) => !this.IsEmpty() && _tokens[0].Type.Equals(type);

    private void Warn(string msg) => _warnings.Add(msg);

    public List<string> GetWarnings() => _warnings;

    private bool IsEmpty() => _tokens.Count == 0;
}

public class MessageParserException : Exception
{
    public MessageParserException(string msg) : base(msg) { }
}