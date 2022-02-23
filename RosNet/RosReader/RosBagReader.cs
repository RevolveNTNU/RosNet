using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RosNet.DataModel;
using RosNet.Field;
using RosNet.Type;
using RosNet.RosMessageParser;
using ICSharpCode.SharpZipLib.BZip2;

namespace RosNet.RosReader;

/// <summary>
/// Reads a ROSbag
/// </summary>
public static class RosBagReader
{
    //Dictionary containing the names of all header fields in record
    private static readonly Dictionary<OpCode, string[]> HeaderFieldsByOp = new Dictionary<OpCode, string[]>()
    {
        { OpCode.MessageData, new string[] {"conn", "time"} },
        { OpCode.BagHeader,   new string[] {"index_pos", "conn_count", "chunk_count"} },
        { OpCode.IndexData,   new string[] {"ver", "conn", "count"} },
        { OpCode.Chunk,       new string[] {"compression", "size"} },
        { OpCode.ChunkInfo,   new string[] {"ver", "chunk_pos", "start_time", "end_time", "count"} },
        { OpCode.Connection,  new string[] {"conn", "topic"} }
    };

    /// <summary>
    /// Reads a ROSbag-file
    /// </summary>
    /// <returns>ROSbag-object</returns>
    public static RosBag Read(string path)
    {
        if (File.Exists(path))
        {
            using BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));
            var rosBag = new RosBag();
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
            return rosBag;
        }
        else
        {
            throw new FileNotFoundException($"File with path {path} does not exist");
            
        }
    }

    /// <summary>
    /// Reads a connection and sets the data
    /// </summary>
    private static void SetConnectionData(BinaryReader reader, Connection connection)
    {
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
                    messageDefinition = MessageDefinitionParser.ParseMessageDefinition(fieldValue);
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
    private static byte[] ReadMessageData(BinaryReader reader)
    {
        var dataLength = reader.ReadInt32();
        var data = reader.ReadBytes(dataLength);
        return data;
    }

    /// <summary>
    /// Reads chunk and adds all messages and message data to unParsedMessageHandler
    /// </summary>
    /// <returns>List of connections in chunk</returns>
    private static List<Connection> ReadUnCompressedChunk(BinaryReader reader, UnParsedMessageHandler unParsedMessageHandler)
    {
        int dataLength = reader.ReadInt32();
        List<Connection> connections = ReadChunk(reader, unParsedMessageHandler, dataLength);

        return connections;

    }

    /// <summary>
    /// Decompresses and reads chunk and adds all messages and message data to unParsedMessageHandler
    /// </summary>
    /// <returns>List of connections in chunk</returns>
    private static List<Connection> ReadCompressedChunk(BinaryReader reader, UnParsedMessageHandler unParsedMessageHandler)
    {
        int compressedDataLength = reader.ReadInt32();
        byte[] data = reader.ReadBytes(compressedDataLength);
        byte[] unCompressedData = Array.Empty<byte>();
        using MemoryStream source = new MemoryStream(data);
        using MemoryStream target = new MemoryStream(compressedDataLength);
        BZip2.Decompress(source, target, false);
        unCompressedData = target.ToArray();

        var connections = new List<Connection>();
        using (BinaryReader tempReader = new BinaryReader(new MemoryStream(unCompressedData)))
        {
            connections = ReadChunk(tempReader, unParsedMessageHandler, unCompressedData.Length);
        }

        return connections;

    }

    /// <summary>
    /// Reads chunk and adds all messages and message data to unParsedMessageHandler
    /// </summary>
    /// <returns>List of connections in chunk</returns>
    private static List<Connection> ReadChunk(BinaryReader reader, UnParsedMessageHandler unParsedMessageHandler, int dataLength)
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
    private static Dictionary<string, FieldValue> ReadHeader(BinaryReader reader)
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

        if (!headerFields.ContainsKey("op"))
            //TODO: More descriptive exception
            throw new Exception("Header is missing op definition");

        foreach (string headerField in HeaderFieldsByOp[(OpCode)headerFields["op"].Value.First()])
        {
            if (!headerFields.ContainsKey(headerField))
            {
                //TODO: Better exception
                throw new Exception($"Missing header field {headerField}");
            }
        }

        return headerFields;
    }

    /// <summary>
    /// Reads a field
    /// </summary>
    /// <returns>FieldValue containing name, datatype and value of field</returns>
    private static FieldValue ReadField(BinaryReader reader)
    {
        int fieldLen = reader.ReadInt32();
        string fieldName = ReadName(reader, fieldLen);

        PrimitiveType dataType;
        byte[] fieldValue;
        switch (fieldName)
        {
            case "index_pos":
            case "time":
            case "chunk_pos":
            case "start_time":
            case "end_time":
                dataType = PrimitiveType.INT64;
                fieldValue = reader.ReadBytes(8);
                break;
            case "conn_count":
            case "chunk_count":
            case "size":
            case "conn":
            case "ver":
            case "count":
            case "offset":
                dataType = PrimitiveType.INT32;
                fieldValue = reader.ReadBytes(4);
                break;
            case "op":
                dataType = PrimitiveType.INT8;
                fieldValue = reader.ReadBytes(1);
                break;
            case "compression":
                dataType = PrimitiveType.STRING;
                char firstChar = reader.ReadChar();
                if (firstChar.Equals('n'))
                {
                    reader.ReadChars(3);
                    fieldValue = Encoding.ASCII.GetBytes("none");
                }
                else
                {
                    reader.ReadChars(2);
                    fieldValue = Encoding.ASCII.GetBytes("bz2");
                }
                break;
            case "topic":
                dataType = PrimitiveType.STRING;
                fieldValue = Encoding.ASCII.GetBytes(new string(reader.ReadChars(fieldLen - 6)));
                break;
            default:
                throw new Exception($"{fieldName} not defined in ROSbag-format");
        }
        return new FieldValue(fieldName, dataType, fieldValue);
    }

    /// <summary>
    /// Reads until first "=", returning what was read, discarding "="
    /// </summary>
    /// <returns> name of field </returns>
    private static string ReadName(BinaryReader reader, int fieldLen)
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
