using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ROSNET.DataModel;
using ROSNET.Field;
using ROSNET.ROSMessageParser;

namespace ROSNET.ROSReader
{
    public static class DataReader
    {
        public static (byte[], byte[], byte[], List<FieldValue>, byte[], byte[]) ReadConnectionData(BinaryReader reader)
        {
            int dataLen = reader.ReadInt32();
            long endPos = reader.BaseStream.Position + dataLen;

            byte[] OriginalTopic = null;
            byte[] Type = null;
            byte[] Md5sum = null;
            List<FieldValue> MessageDefinition = null;
            byte[] CallerID = null;
            byte[] Latching = null;
            while (reader.BaseStream.Position != endPos)
            {
                var fieldLen = reader.ReadInt32();
                var fieldName = Header.ReadName(reader);
                var fieldValue = reader.ReadBytes(fieldLen - fieldName.Length - 1);

                switch (fieldName)
                {
                    case "topic":
                        OriginalTopic = fieldValue;
                        break;
                    case "type":
                        Type = fieldValue;
                        break;
                    case "md5sum":
                        Md5sum = fieldValue;
                        break;
                    case "message_definition":
                        MessageDefinition = MessageDefinitionParser.ParseMessageDefinition(fieldValue);
                        break;
                    case "callerid":
                        CallerID = fieldValue;
                        break;
                    case "latching":
                        Latching = fieldValue;
                        break;
                }

            }
            return (OriginalTopic, Type, Md5sum, MessageDefinition, CallerID, Latching);
        }

           public static byte[] ReadMessageData(BinaryReader reader)
            {
                var dataLength = reader.ReadInt32();
                var data = reader.ReadBytes(dataLength);
                return data;
            }

        public static List<Connection> ReadChunk(BinaryReader reader, UnParsedMessageHandler unParsedMessageHandler)
        {
            int dataLength = reader.ReadInt32();
            long endPos = reader.BaseStream.Position + dataLength;

            var connections = new List<Connection>();

            Dictionary<string, FieldValue> header;
            while (reader.BaseStream.Position != endPos)
            {
                header = Header.readHeader(reader);

                switch ((int) header["op"].Value.First())
                {
                    case 7:
                        var connection = new Connection(header["conn"], header["topic"]);
                        (byte[] originalTopic, byte[] type, byte[] md5sum, List<FieldValue> messageDefinition, byte[] callerID, byte[] latching) = DataReader.ReadConnectionData(reader);
                        connection.SetData(originalTopic, type, md5sum, messageDefinition, callerID, latching);
                        connections.Add(connection);
                        break;
                    case 2:
                        var message = new Message(header["conn"], header["time"]);
                        var data = DataReader.ReadMessageData(reader);
                        unParsedMessageHandler.AddUnParsedMessage(message, data);
                        break;
                    default:
                        var dataLen = reader.ReadInt32();
                        reader.ReadBytes(dataLen);
                        break;
                }
            }
            return connections;
        }
    }


}
