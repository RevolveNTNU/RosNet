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


public class MessageTokenizer : IDisposable
{

    private readonly string _inFilePath;
    private uint _lineNum = 1;

    private readonly StreamReader _reader;

    private readonly HashSet<string> _builtInTypes;

    private readonly HashSet<char> _allowedSpecialCharacterForTypeIdentifier = new()
    { '_', '/' };

    public MessageTokenizer(string inFilePath, HashSet<string> builtInTypes)
    {
        this._inFilePath = inFilePath;
        this._reader = new StreamReader(inFilePath);
        this._builtInTypes = builtInTypes;
    }

    /// <summary>
    /// Tokenizes the stream input
    /// </summary>
    /// <returns> A list of MessageTokens read from the stream </returns>
    public List<List<MessageToken>> Tokenize()
    {
        List<List<MessageToken>> listsOfTokens = new();

        listsOfTokens.Add(new List<MessageToken>());
        int listIndex = 0;

        // Information about the original file
        listsOfTokens[0].Add(new MessageToken(MessageTokenType.FilePath, _inFilePath, 0));

        while (!_reader.EndOfStream)
        {
            DiscardEmpty();
            if (_reader.EndOfStream)
            {
                break;
            }
            if (_reader.Peek() == '\n')
            {
                // If line is empty, move on
                _reader.Read();
                _lineNum++;
                continue;
            }
            else if (_reader.Peek() == '\r')
            {
                // CRLF new line for Windows
                _reader.Read();
                if (_reader.Peek() == '\n')
                {
                    _reader.Read();
                    _lineNum++;
                }
                continue;
            }
            else if (_reader.Peek() == '#')
            {
                // A line that starts with a '#' is a comment
                listsOfTokens[listIndex].Add(NextCommentToken());
            }
            else if (_reader.Peek() == '-')
            {
                // Seperator ---
                NextSeperator();
                listsOfTokens.Add(new List<MessageToken>());
                listIndex++;
                listsOfTokens[listIndex].Add(new MessageToken(MessageTokenType.FilePath, _inFilePath, 0));
            }
            else
            {
                // Otherwise a declaration
                // Note that a string constant line cannot have comment

                // Line always start with a type
                // Type can be built-in, defined, list, or Header
                listsOfTokens[listIndex].Add(NextTypeIdentifierToken());

                // If peek shows '[', the type is a list/array
                if (_reader.Peek() == '[')
                {
                    listsOfTokens[listIndex].Add(NextArrayDeclaration());
                }
                DiscardEmpty();

                // Then, field identifier
                listsOfTokens[listIndex].Add(NextIdentifierToken());
                DiscardEmpty();

                // A constant may be declared
                if (_reader.Peek() == '=')
                {
                    listsOfTokens[listIndex].Add(NextConstantDeclaration());
                }

                // Optionally, the line may have a comment line
                if (_reader.Peek() == '#')
                {
                    listsOfTokens[listIndex].Add(NextCommentToken());
                }
                else
                {
                    // The line ends with spaces to be discarded and a '\n'
                    DiscardEmpty();
                    if (_reader.Peek() == '\n')
                    {
                        _reader.Read();
                        _lineNum++;
                    }
                    else if (_reader.Peek() == '\r')
                    {
                        // CRLF new line for Windows
                        _reader.Read();
                        if (_reader.Peek() == '\n')
                        {
                            _reader.Read();
                            _lineNum++;
                        }
                    }
                    else if (!_reader.EndOfStream)
                    {
                        throw new MessageTokenizerException(
                            $"Invalid token: {NextTokenStr()}. New line or EOF expected {CurrentFileAndLine()}");
                    }
                }
            }
        }
        return listsOfTokens;
    }

    /// <summary>
    /// Read and discards empty spaces
    /// Empty spaces include ' ' and '\t'
    /// </summary>
    private void DiscardEmpty()
    {
        while (_reader.Peek() is ' ' or '\t')
        {
            _reader.Read();
        }
    }

    /// <summary>
    /// Read until '\n' and return all content before '\n'
    /// Removes start and end trailing spaces
    /// </summary>
    /// <returns> All content before '\n' </returns>
    private string ReadUntilNewLineAndTrim()
    {
        string content = "";
        while (_reader.Peek() != '\n' && !_reader.EndOfStream)
        {
            if (_reader.Peek() == '\r')
            {
                // Discard carriage return
                _reader.Read();
            }
            else
            {
                content += (char)_reader.Read();
            }
        }
        return content.Trim();
    }

    /// <summary>
    /// Returns the next token string
    /// Tokens are seperated by whitespace (" " or "\n")
    /// </summary>
    /// <returns> Next token string in the stream </returns>
    private string NextTokenStr()
    {
        string token = "";
        while (!(_reader.Peek() is ' ' or '\n' || _reader.EndOfStream))
        {
            if (_reader.Peek() == '\r')
            {
                // Discard carriage return
                _reader.Read();
            }
            else
            {
                token += (char)_reader.Read();
            }
        }
        _reader.Read();
        return token;
    }

    /// <summary>
    /// Returns the next comment token
    /// A comment is defined as "# sigma* \n"
    /// Assumes a '#' has been peeked
    /// </summary>
    /// <returns> Next comment token in the stream </returns>
    private MessageToken NextCommentToken()
    {
        _reader.Read(); // Discard '#'

        string comment = "";
        while (_reader.Peek() != '\n' && !_reader.EndOfStream)
        {
            if (_reader.Peek() == '\r')
            {
                // Discard carriage return
                _reader.Read();
            }
            else
            {
                comment += (char)_reader.Read();
            }
        }
        _reader.Read();
        _lineNum++;
        return new MessageToken(MessageTokenType.Comment, comment, _lineNum - 1);
    }

    /// <summary>
    /// Returns the next ROS Service/Action seperator
    /// Only allows "---" on its own line
    /// </summary>
    /// <returns> Next seperator token in the stream </returns>
    private MessageToken NextSeperator()
    {
        string token = ReadUntilNewLineAndTrim();
        if (token.Equals("---", StringComparison.Ordinal))
        {
            _reader.Read();
            _lineNum++;
            return new MessageToken(MessageTokenType.Seperator, token, _lineNum - 1);
        }
        else
        {
            throw new MessageTokenizerException($"Unexpected token '{token}'. Did you mean '---' (ROS Service/Action seperator)?");
        }
    }

    /// <summary>
    /// Returns the next type identifier token
    /// Type identifiers start with an alphabet and can contain _ and /
    /// Array notation is considered a seperate token
    /// </summary>
    /// <returns> Next type identifer token in the stream </returns>
    private MessageToken NextTypeIdentifierToken()
    {
        string tokenStr = "";

        // If start char is not alphabet, identifier invalid
        if (!char.IsLetter((char)_reader.Peek()))
        {
            throw new MessageTokenizerException($"Invalid type identifier: {NextTokenStr()} {CurrentFileAndLine()}");
        }

        // Otherwise, consume input until seperator, EOF or '['
        while (_reader.Peek() != ' ' && _reader.Peek() != '[' && !_reader.EndOfStream)
        {
            if (!char.IsLetterOrDigit((char)_reader.Peek()) && !_allowedSpecialCharacterForTypeIdentifier.Contains((char)_reader.Peek()))
            {
                throw new MessageTokenizerException($"Invalid character in type identifier: {(char)_reader.Peek()} {CurrentFileAndLine()}");
            }
            tokenStr += (char)_reader.Read();
        }

        var tokenType = true switch
        {
            true when _builtInTypes.Contains(tokenStr) => MessageTokenType.BuiltInType,
            true when tokenStr == "Header" => MessageTokenType.Header,
            _ => MessageTokenType.DefinedType,
        };
        return new MessageToken(tokenType, tokenStr, _lineNum);
    }

    /// <summary>
    /// Returns the next array declaration
    /// Array declarations are defined as [] or [number]
    /// Assumes that '[' has been peeked
    /// </summary>
    /// <returns> Next array declaration token in the stream </returns>
    private MessageToken NextArrayDeclaration()
    {
        string tokenStr = "";

        _reader.Read(); // Discard '['

        if (_reader.Peek() == ']')
        {
            _reader.Read(); // Discard ']'
            if (_reader.Peek() != ' ')
            {
                throw new MessageTokenizerException($"Invalid character '{(char)_reader.Peek()}' after ']' {CurrentFileAndLine()}");
            }
            return new MessageToken(MessageTokenType.VariableSizeArray, "", _lineNum);
        }
        else
        {
            string arraySizeStr = "";
            while (_reader.Peek() != ']')
            {
                arraySizeStr += (char)_reader.Read();
            }
            if (uint.TryParse(arraySizeStr, out uint arraySize))
            {
                tokenStr += arraySize;
            }
            else
            {
                // Invalid Array Declaration
                throw new MessageTokenizerException($"Invalid array declaration: [{arraySizeStr}] {CurrentFileAndLine()}");
            }

            _reader.Read(); // Discard ']'

            if (_reader.Peek() != ' ')
            {
                throw new MessageTokenizerException($"Invalid character '{(char)_reader.Peek()}' after ']' {CurrentFileAndLine()}");
            }
            return new MessageToken(MessageTokenType.FixedSizeArray, tokenStr, _lineNum);
        }
    }

    /// <summary>
    /// Returns the next field identifier token
    /// Field identifiers can only start with an alphabet
    /// </summary>
    /// <returns> Next field identifier token in the stream </returns>
    private MessageToken NextIdentifierToken()
    {
        // If start char is not alphabet, identifier invalid
        if (!char.IsLetter((char)_reader.Peek()))
        {
            throw new MessageTokenizerException($"Invalid identifier: {NextTokenStr()} {CurrentFileAndLine()}");
        }

        string tokenStr = "";
        // Otherwise, consume input until seperator or EOF
        while (!(_reader.Peek() is ' ' or '\n' or '=' || _reader.EndOfStream))
        {
            if (_reader.Peek() == '\r')
            {
                _reader.Read();
                continue;
            }
            if (!char.IsLetterOrDigit((char)_reader.Peek()) && _reader.Peek() != '_')
            {
                throw new MessageTokenizerException($"Invalid character in identifier: {(char)_reader.Peek()} {CurrentFileAndLine()}");
            }
            tokenStr += (char)_reader.Read();
        }

        return new MessageToken(MessageTokenType.Identifier, tokenStr, _lineNum);
    }

    /// <summary>
    /// Returns the next constant declaration
    /// Will decide declaration type
    /// Assumes that '=' has been peeked
    /// </summary>
    /// <returns></returns>
    private MessageToken NextConstantDeclaration()
    {
        _reader.Read();

        string val = ReadUntilNewLineAndTrim();

        return new MessageToken(MessageTokenType.ConstantDeclaration, val, _lineNum);
    }

    /// <summary>
    /// Returns the current file path and line number
    /// </summary>
    /// <returns> Returns the current file path and line number </returns>
    private string CurrentFileAndLine() => $"({this._inFilePath}:{_lineNum})";

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        ((IDisposable)_reader).Dispose();
    }
}

public class MessageTokenizerException : Exception
{
    public MessageTokenizerException(string msg) : base(msg) { }
}
