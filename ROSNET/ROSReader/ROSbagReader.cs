using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ROSNET.DataModel;
using ROSNET.Field;
using ROSNET.ROSMessageParser;

namespace ROSNET.ROSReader
{
    public static class ROSbagReader
    {
        public static ROSbag Read(string path)
        {
            if (File.Exists(path))
            {
                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    ROSbag rosbag = new ROSbag();
                    UnParsedMessageHandler unParsedMessageHandler = new UnParsedMessageHandler();

                    Console.Write(reader.ReadChars(13)); //reads inital line of ROSbag

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        var header = Header.readHeader(reader);
                        
                        switch (Convert.ToInt32(header["op"].Value.Last()))
                        {
                            case 2:
                                var message = new Message(header["conn"], header["time"]);
                                var data = DataReader.ReadMessageData(reader);
                                unParsedMessageHandler.AddUnParsedMessage(message, data);
                                break;
                            case 5:
                                //TODO hva med compression bz2
                                var chunkConnections = DataReader.ReadChunk(reader, unParsedMessageHandler);
                                chunkConnections.Where(c => rosbag.AddConnection(c));

                                break;
                            case 7:
                                var connection = new Connection(header["conn"], header["topic"]);
                                (byte[] originalTopic, byte[] type, byte[] md5sum, List<FieldValue> messageDefinition, byte[] callerID, byte[] latching) = DataReader.ReadConnectionData(reader);
                                connection.SetData(originalTopic, type, md5sum, messageDefinition, callerID, latching);
                                rosbag.AddConnection(connection);
                                break;
                            default:
                                int dataLength = reader.ReadInt32();
                                reader.ReadBytes(dataLength);
                                break;
                        }
                    }
                    unParsedMessageHandler.ParseMessages(rosbag);

                    
                    Console.WriteLine(rosbag.ToString());
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
