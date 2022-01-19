using System;
using System.Collections.Generic;
using System.IO;
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

                    Console.Write(reader.ReadChars(13)); //reads inital line of ROSbag

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        Dictionary<string, FieldValue> header = Header.readHeader(reader);

                        //Console.WriteLine("Header: ");
                        foreach (KeyValuePair<string, FieldValue> kvp in header)
                        {
                            //Console.WriteLine(kvp.Key + kvp.Value);
                        }
                        
                        int conn;
                        switch (Convert.ToInt32(header["op"].Value[header["op"].Value.Length - 1]))
                        {
                            case 5:
                                //TODO hva med compression bz2
                                Console.WriteLine("Compression type: " + System.Text.Encoding.Default.GetString(header["compression"].Value)); 

                                Tuple<List<Connection>, List<Tuple<Message, byte[]>>> chunkData = ReadChunk(reader);
                                foreach(Tuple<Message,byte[]> chunkMessage in chunkData.Item2)
                                {
                                    if (rosbag.Connections.ContainsKey(chunkMessage.Item1.Conn))
                                    {
                                        Message message = chunkMessage.Item1;
                                        Console.WriteLine($"Sets data of Message with conn: {message.Conn}");
                                        message.SetData(chunkMessage.Item2, rosbag.Connections[chunkMessage.Item1.Conn].MessageDefinition);
                                        rosbag.AddMessage(message);
                                        
                                    }
                                    else
                                    {
                                        rosbag.AddUnReadMessage(chunkMessage.Item1, chunkMessage.Item2);
                                    }
                                }
                                foreach(Connection chunkConnection in chunkData.Item1)
                                {
                                    rosbag.AddConnection(chunkConnection);
                                }
                                
                                
                                break;
                            case 7:
                                conn = BitConverter.ToInt32(header.GetValueOrDefault("conn").Value);
                                Connection connection = new Connection(header.GetValueOrDefault("conn"), header.GetValueOrDefault("topic"), reader);
                                rosbag.AddConnection(connection);
                                break;
                            case 2:
                                conn = BitConverter.ToInt32(header.GetValueOrDefault("conn").Value);
                                Console.WriteLine("New Message: ");
                                if (rosbag.Connections.ContainsKey(conn))
                                {
                                    Message message = new Message(header.GetValueOrDefault("conn"), header.GetValueOrDefault("time"), reader, rosbag.Connections.GetValueOrDefault(conn).MessageDefinition);
                                    rosbag.AddMessage(message);
                                
                                }
                                else
                                {
                                    Message message = new Message(header.GetValueOrDefault("conn"), header.GetValueOrDefault("time"));
                                    int dataLength = reader.ReadInt32();
                                    byte[] data = reader.ReadBytes(dataLength);
                                    rosbag.AddUnReadMessage(message, data);
                                }
                                break;
                            default:
                                int dataLen = reader.ReadInt32();
                                reader.ReadBytes(dataLen);
                                break;
                        }
                    }
                    if (rosbag.UnReadMessages.Count !=0)
                    {
                        //TODO
                        Console.WriteLine("UnReadMessages: ");
                        foreach (KeyValuePair<int, List<Tuple<Message,byte[]>>> kvp in rosbag.UnReadMessages)
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
                    Console.WriteLine(rosbag.toString());
                    return rosbag;
                }
                
            }
            else
            {
                throw new FileNotFoundException("File does not exist");
            }
        }

        private static Tuple<List<Connection>,List<Tuple<Message,byte[]>>> ReadChunk(BinaryReader reader)
        {
            int dataLen = reader.ReadInt32();
            long endPos = reader.BaseStream.Position + dataLen;

            List<Connection> connections = new List<Connection>();
            List<Tuple<Message,byte[]>> messages = new List<Tuple<Message, byte[]>>();

            while (reader.BaseStream.Position != endPos)
            {
                Dictionary<string, FieldValue> header = Header.readHeader(reader);

                switch (Convert.ToInt32(header["op"].Value[header["op"].Value.Length - 1]))
                {
                    case 7:
                        Connection connection = new Connection(header.GetValueOrDefault("conn"), header.GetValueOrDefault("topic"), reader);
                        connections.Add(connection);
                        break;
                    case 2:
                        Message message = new Message(header["conn"], header["time"]);
                        var dataLength = reader.ReadInt32();
                        byte[] data = reader.ReadBytes(dataLength);
                        messages.Add(new Tuple<Message, byte[]>(message, data));
                        break;
                    default:
                        var dataLe = reader.ReadInt32();
                        reader.ReadBytes(dataLen);
                        break;
                }
            }
            return new Tuple<List<Connection>, List<Tuple<Message, byte[]>>>(connections,messages);
    }

       

    }
}
