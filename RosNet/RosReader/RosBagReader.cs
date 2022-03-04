using ICSharpCode.SharpZipLib.BZip2;
using RosNet.DataModel;
using RosNet.Field;
using RosNet.RosMessageParser;
using RosNet.Type;

namespace RosNet.RosReader;

/// <summary>
/// Reads a ROSbag
/// </summary>
internal class RosBagReader
{

    // Represents the different Op codes within the RosBag 2.0 format, with their respective bytes
    public enum OpCode
    {
        MessageData = 0x02,
        BagHeader = 0x03,
        IndexData = 0x04,
        Chunk = 0x05,
        ChunkInfo = 0x06,
        Connection = 0x07
    }

    //Dictionary containing the names of all header fields in record
    private readonly Dictionary<OpCode, string[]> HeaderFieldsByOp = new Dictionary<OpCode, string[]>()
    {
        { OpCode.MessageData, new string[] {"conn", "time"} },
        { OpCode.BagHeader,   new string[] {"index_pos", "conn_count", "chunk_count"} },
        { OpCode.IndexData,   new string[] {"ver", "conn", "count"} },
        { OpCode.Chunk,       new string[] {"compression", "size"} },
        { OpCode.ChunkInfo,   new string[] {"ver", "chunk_pos", "start_time", "end_time", "count"} },
        { OpCode.Connection,  new string[] {"conn", "topic"} }
    };

    public RosBagReader()
    {

    }

    /// <summary>
    /// Reads a ROSbag-file
    /// </summary>
    /// <returns>ROSbag-object</returns>
    public void Read(RosBag rosBag)
    {
        if (!File.Exists(rosBag.Path))
        {
            throw new FileNotFoundException($"File with path {rosBag.Path} does not exist");
        }
        using BinaryReader reader = new BinaryReader(File.Open(rosBag.Path, FileMode.Open));
        var unParsedMessageHandler = new UnParsedMessageHandler(); //handles all message data

        reader.ReadChars(13);

        while (reader.BaseStream.Position != reader.BaseStream.Length)
        {
            Dictionary<String, FieldValue> header = ReadHeader(reader);

            //reads record based on op value in header
            switch ((OpCode)header["op"].Value.First())
            {
                case OpCode.MessageData:
                    var message = new Message(header["conn"], header["time"]);
                    byte[] data = ReadMessageData(reader);
                    unParsedMessageHandler.AddUnParsedMessage(message, data);
                    break;
                case OpCode.Chunk:
                    var chunkConnections = new List<Connection>();
                    if (header["compression"].Value.Length == 3)
                    {
                        chunkConnections = ReadCompressedChunk(reader, unParsedMessageHandler);
                    }
                    else
                    {
                        chunkConnections = ReadUnCompressedChunk(reader, unParsedMessageHandler);
                    }
                    chunkConnections.Where(c => rosBag.AddConnection(c));
                    break;
                case OpCode.Connection:
                    var connection = new Connection(header["conn"], header["topic"]);
                    SetConnectionData(reader, connection);
                    rosBag.AddConnection(connection);
                    break;
                default: //Other record types
                    int dataLength = reader.ReadInt32();
                    reader.ReadBytes(dataLength);
                    break;
            } 
        }
        unParsedMessageHandler.ParseMessages(rosBag); //Parses all message data  
    }

    /// <summary>
    /// Reads a connection and sets the data
    /// </summary>
    private void SetConnectionData(BinaryReader reader, Connection connection)
    {
        var messageDefinitionParser = new MessageDefinitionParser();
        int dataLen = reader.ReadInt32();
        long endPos = reader.BaseStream.Position + dataLen;

        byte[] originalTopic = null;
        byte[] type = null;
        byte[] md5sum = null;
        List<FieldValue> messageDefinition = null;
        byte[] callerID = null;
        byte[] latching = null;
        while (reader.BaseStream.Position != endPos)
        {
            int fieldLen = reader.ReadInt32();
            string fieldName = ReadName(reader, fieldLen);
            byte[] fieldValue = reader.ReadBytes(fieldLen - fieldName.Length - 1);

            switch (fieldName)
            {
                case "topic":
                    originalTopic = fieldValue;
                    break;
                case "type":
                    type = fieldValue;
                    break;
                case "md5sum":
                    md5sum = fieldValue;
                    break;
                case "message_definition":
                    messageDefinition = messageDefinitionParser.ParseMessageDefinition(fieldValue);
                    break;
                case "callerid":
                    callerID = fieldValue;
                    break;
                case "latching":
                    latching = fieldValue;
                    break;
            }

        }
        connection.SetData(originalTopic, type, md5sum, messageDefinition, callerID, latching);
    }

    /// <summary>
    /// Reads message data
    /// </summary>
    /// <returns>message data in bytes</returns>
    private byte[] ReadMessageData(BinaryReader reader)
    {
        var dataLength = reader.ReadInt32();
        var data = reader.ReadBytes(dataLength);
        return data;
    }

    /// <summary>
    /// Reads chunk and adds all messages and message data to unParsedMessageHandler
    /// </summary>
    /// <returns>List of connections in chunk</returns>
    private List<Connection> ReadUnCompressedChunk(BinaryReader reader, UnParsedMessageHandler unParsedMessageHandler)
    {
        int dataLength = reader.ReadInt32();
        List<Connection> connections = ReadChunk(reader, unParsedMessageHandler, dataLength);

        return connections;

    }

    /// <summary>
    /// Decompresses and reads chunk and adds all messages and message data to unParsedMessageHandler
    /// </summary>
    /// <returns>List of connections in chunk</returns>
    private List<Connection> ReadCompressedChunk(BinaryReader reader, UnParsedMessageHandler unParsedMessageHandler)
    {
        int compressedDataLength = reader.ReadInt32();
        byte[] data = reader.ReadBytes(compressedDataLength);
        using var source = new MemoryStream(data);
        using var uncompressed = new MemoryStream(compressedDataLength);
        BZip2.Decompress(source, uncompressed, false);

        using var uncompressedReader = new BinaryReader(uncompressed);
        uncompressedReader.BaseStream.Seek(0, SeekOrigin.Begin);
        var connections = ReadChunk(uncompressedReader, unParsedMessageHandler, (int)uncompressed.Length);

        return connections;

    }

    /// <summary>
    /// Reads chunk and adds all messages and message data to unParsedMessageHandler
    /// </summary>
    /// <returns>List of connections in chunk</returns>
    private List<Connection> ReadChunk(BinaryReader reader, UnParsedMessageHandler unParsedMessageHandler, int dataLength)
    {
        long endPos = reader.BaseStream.Position + dataLength;

        var connections = new List<Connection>();

        while (reader.BaseStream.Position != endPos)
        {
            Dictionary<string, FieldValue> header = ReadHeader(reader);

            switch ((OpCode)header["op"].Value.First())
            {
                case OpCode.MessageData:
                    var message = new Message(header["conn"], header["time"]);
                    var data = ReadMessageData(reader);
                    unParsedMessageHandler.AddUnParsedMessage(message, data);
                    break;
                case OpCode.Connection:
                    var connection = new Connection(header["conn"], header["topic"]);
                    SetConnectionData(reader, connection);
                    connections.Add(connection);
                    break;
                default:
                    var dataLen = reader.ReadInt32();
                    reader.ReadBytes(dataLen);
                    break;
            }
        }
        return connections;

    }

    /// <summary>
    /// Reads a header
    /// </summary>
    /// <returns>Dictionary with fieldnames and fieldvalues in header</returns>
    private Dictionary<string, FieldValue> ReadHeader(BinaryReader reader)
    {
        int headerLen = reader.ReadInt32();
        long headerEnd = reader.BaseStream.Position + headerLen;
        var headerFields = new Dictionary<string, FieldValue>();

        //checks if all fields in header are read
        while (reader.BaseStream.Position < headerEnd)
        {
            FieldValue fieldValue = ReadField(reader);
            headerFields.Add(fieldValue.Name, fieldValue);
        }

        if (!headerFields.TryGetValue("op", out var op))
            //TODO: More descriptive exception
            throw new Exception("Header is missing op definition");

        if (!HeaderFieldsByOp[(OpCode)op.Value.First()].All(h => headerFields.ContainsKey(h)))
        {
            //TODO: Better exception
            throw new Exception($"Missing header field");
        }

        return headerFields;
    }

    /// <summary>
    /// Reads a field
    /// </summary>
    /// <returns>FieldValue containing name, datatype and value of field</returns>
    private FieldValue ReadField(BinaryReader reader)
    {
        int fieldLen = reader.ReadInt32();
        string fieldName = ReadName(reader, fieldLen);

        byte[] fieldValue = reader.ReadBytes(fieldLen - fieldName.Length - 1);
        var dataType = fieldName switch
        {
            "index_pos" or "time" or "chunk_pos" or "start_time" or "end_time" => PrimitiveType.INT64,
            "conn_count" or "chunk_count" or "size" or "conn" or "ver" or "count" or "offset" => PrimitiveType.INT32,
            "op" => PrimitiveType.UINT8,
            "compression" or "topic" => PrimitiveType.STRING,
            _ => throw new Exception($"{fieldName} not defined in ROSbag-format")
        };

        return new FieldValue(fieldName, dataType, fieldValue);
    }

    /// <summary>
    /// Reads until first "=", returning what was read, discarding "="
    /// </summary>
    /// <returns> name of field </returns>
    private string ReadName(BinaryReader reader, int fieldLen)
    {
        long fieldEndPos = reader.BaseStream.Position + fieldLen;
        char curChar;
        string fieldName = "";
        do
        {
            curChar = reader.ReadChar();
            if (curChar != '=')
            {
                fieldName += curChar;
            }

            if (reader.BaseStream.Position == fieldEndPos)
                //TODO: More descriptive exception
                throw new Exception($"Field \"{fieldName}\" exceeds the field length of {fieldLen}");
        }
        while (curChar != '=');

        return fieldName;
    }
}
