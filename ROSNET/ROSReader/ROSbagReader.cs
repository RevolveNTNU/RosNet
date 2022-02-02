using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ROSNET.DataModel;
using ROSNET.Field;

namespace ROSNET.ROSReader
{
    /// <summary>
    /// Reads ROSbag
    /// </summary>
    public static class ROSbagReader
    {
        /// <summary>
        /// Reads a ROSbag-file
        /// </summary>
        /// <returns>ROSbag-object</returns>
        public static ROSbag Read(string path)
        {
            if (File.Exists(path))
            {
                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    var rosbag = new ROSbag();
                    var unParsedMessageHandler = new UnParsedMessageHandler(); //handles all message data

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        Dictionary<String,FieldValue> header = Header.readHeader(reader);

                        //reads record based on op value in header
                        switch ((int)header["op"].Value.First())
                        {
                            case 2: //Message
                                var message = new Message(header["conn"], header["time"]);
                                byte[] data = DataReader.ReadMessageData(reader);
                                unParsedMessageHandler.AddUnParsedMessage(message, data);
                                break;
                            case 5: //Chunk
                                //decompress chunks with compression bz2 here
                                List<Connection> chunkConnections = DataReader.ReadChunk(reader, ref unParsedMessageHandler);
                                chunkConnections.Where(c => rosbag.AddConnection(c));
                                break;
                            case 7: //Connection
                                var connection = new Connection(header["conn"], header["topic"]);
                                DataReader.SetConnectionData(reader, ref connection);
                                rosbag.AddConnection(connection);
                                break;
                            default: //Other record
                                int dataLength = reader.ReadInt32();
                                reader.ReadBytes(dataLength);
                                break;
                        }
                    }
                    unParsedMessageHandler.ParseMessages(rosbag); //Parses all message data
                    return rosbag;
                }
                
            }
            else
            {
                throw new FileNotFoundException("File does not exist");
            }
        }
    }
}
