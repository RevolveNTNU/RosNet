using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RosNet.DataModel;
using RosNet.Field;

namespace RosNet.RosReader
{
    /// <summary>
    /// Reads a ROSbag
    /// </summary>
    public static class RosBagReader
    {
        /// <summary>
        /// Reads a ROSbag-file
        /// </summary>
        /// <returns>ROSbag-object</returns>
        public static RosBag Read(string path)
        {
            if (File.Exists(path))
            {
                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    var rosBag = new RosBag();
                    var unParsedMessageHandler = new UnParsedMessageHandler(); //handles all message data

                    reader.ReadChars(13);

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        Dictionary<String,FieldValue> header = Header.ReadHeader(reader);

                        //reads record based on op value in header
                        switch ((int)header["op"].Value.First())
                        {
                            case 2: //Message
                                var message = new Message(header["conn"], header["time"]);
                                byte[] data = DataReader.ReadMessageData(reader);
                                unParsedMessageHandler.AddUnParsedMessage(message, data);
                                break;
                            case 5: //Chunk
                                var chunkConnections = new List<Connection>();
                                if (header["compression"].Value.Length == 3)
                                {
                                    chunkConnections = DataReader.ReadCompressedChunk(reader, ref unParsedMessageHandler);
                                } else
                                {
                                    chunkConnections = DataReader.ReadUnCompressedChunk(reader, ref unParsedMessageHandler);
                                }
                                chunkConnections.Where(c => rosBag.AddConnection(c));
                                break;
                            case 7: //Connection
                                var connection = new Connection(header["conn"], header["topic"]);
                                DataReader.SetConnectionData(reader, ref connection);
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
            }
            else
            {
                throw new FileNotFoundException($"File with path {path} does not exist");
            }
        }
    }
}
