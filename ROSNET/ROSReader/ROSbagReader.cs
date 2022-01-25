using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ROSNET.DataModel;
using ROSNET.Field;
using ROSNET.Reader;

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
                    UnReadMessageHandler unReadMessageHandler = new UnReadMessageHandler(rosbag);

                    Console.Write(reader.ReadChars(13)); //reads inital line of ROSbag

                    Dictionary<string, FieldValue> header;

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        header = Header.readHeader(reader);
                        
                        int conn;
                        switch (Convert.ToInt32(header["op"].Value.Last()))
                        {
                            case 2:
                                conn = BitConverter.ToInt32(header["conn"].Value);
                                if (rosbag.Connections.ContainsKey(conn))
                                {
                                    var message = new Message(header["conn"], header["time"], reader, rosbag.Connections[conn].MessageDefinition);
                                    rosbag.AddMessage(message);
                                }
                                else
                                {
                                    var message = new Message(header["conn"], header["time"]);
                                    var dataLength = reader.ReadInt32();
                                    var data = reader.ReadBytes(dataLength);
                                    
                                }
                                break;
                            case 5:
                                //TODO hva med compression bz2

                                (var chunkConnections, var chunkMessages) = ReadChunk(reader);
                                foreach(var chunkMessage in chunkMessages)
                                {
                                    (var message, var data) = chunkMessage;
                                    if (rosbag.Connections.ContainsKey(message.Conn))
                                    {
                                        message.SetData(data, rosbag.Connections[message.Conn].MessageDefinition);
                                        rosbag.AddMessage(message);
                                    }
                                    else
                                    {
                                        unReadMessageHandler.AddUnReadMessage(message, data);
                                    }
                                }
                                foreach(var chunkConnection in chunkConnections)
                                {
                                    rosbag.AddConnection(chunkConnection);
                                    unReadMessageHandler.UpdateUnReadMessages(chunkConnection);
                                }

                                break;
                            case 7:
                                var connection = new Connection(header["conn"], header["topic"], reader);
                                rosbag.AddConnection(connection);
                                unReadMessageHandler.UpdateUnReadMessages(connection);
                                Console.Write("Made connection with conn " + header["conn"]);
                                break;
                            default:
                                int dataLen = reader.ReadInt32();
                                reader.ReadBytes(dataLen);
                                break;
                        }
                    }
                    if (unReadMessageHandler.UnReadMessages.Count !=0)
                    {
                        Console.WriteLine("UnReadMessages: ");
                        foreach (KeyValuePair<int, List<(Message,byte[])>> kvp in unReadMessageHandler.UnReadMessages)
                        {
                            Console.WriteLine(kvp.Key + ": " + kvp.Value.Count);
                        }
                        Console.WriteLine("Connections: ");
                        foreach (KeyValuePair<int, Connection> kvp in rosbag.Connections)
                        {
                            Console.WriteLine(kvp.Key);
                        }
                        Console.WriteLine("Messages ");
                        foreach (KeyValuePair<int,List<Message>> kvp in rosbag.Messages)
                        {
                            Console.WriteLine(kvp.Key + ": " + kvp.Value.Count);
                        }
                        throw new Exception("There are messages without the corresponding connection");
                    }
                    Console.WriteLine(rosbag.ToString());
                    return rosbag;
                }
                
            }
            else
            {
                throw new FileNotFoundException("File does not exist");
            }
        }

        private static (List<Connection>,List<(Message,byte[])>) ReadChunk(BinaryReader reader)
        {
            int dataLen = reader.ReadInt32();
            long endPos = reader.BaseStream.Position + dataLen;

            var connections = new List<Connection>();
            var messages = new List<(Message, byte[])>();

            Dictionary<string, FieldValue> header;
            while (reader.BaseStream.Position != endPos)
            {
                header = Header.readHeader(reader);

                switch (Convert.ToInt32(header["op"].Value.Last()))
                {
                    case 7:
                        var connection = new Connection(header["conn"], header["topic"], reader);
                        connections.Add(connection);
                        break;
                    case 2:
                        Message message = new Message(header["conn"], header["time"]);
                        var dataLength = reader.ReadInt32();
                        var data = reader.ReadBytes(dataLength);
                        messages.Add((message, data));
                        break;
                    default:
                        var dataLe = reader.ReadInt32();
                        reader.ReadBytes(dataLen);
                        break;
                }
            }
            return (connections,messages);
        }



    }
}
