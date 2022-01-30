using System;
using System.Collections.Generic;
using ROSNET.DataModel;
using ROSNET.ROSMessageParser;

namespace ROSNET.ROSReader
{
    /// <summary>
    /// Contains message-records with unparsed data because their corresponding connections are not read yet
    /// </summary>
    public class UnParsedMessageHandler
    {
        public Dictionary<int, List<(Message, byte[])>> UnParsedMessages { get; private set; }

        public UnParsedMessageHandler()
        {
            UnParsedMessages = new Dictionary<int, List<(Message, byte[])>>();
        }

        public void AddUnParsedMessage(Message message, byte[] data)
        {
            if (UnParsedMessages.ContainsKey(message.Conn))
            {
                UnParsedMessages[message.Conn].Add((message, data));
            }
            else
            {
                UnParsedMessages.Add(message.Conn, new List<(Message, byte[])>() { (message, data) });
            }
        }

        public void ParseMessages(ROSbag rosbag)
        {
            foreach(KeyValuePair<int, Connection> kvp in rosbag.Connections)
            {
                foreach((var message, var data) in UnParsedMessages[kvp.Key])
                {
                    message.Data = MessageDataParser.ParseMessageData(data, kvp.Value.MessageDefinition);
                    rosbag.AddMessage(message);
                }
                UnParsedMessages.Remove(kvp.Key);
            }

            if (UnParsedMessages.Count != 0)
            {
                Console.WriteLine("UnReadMessages: ");
                foreach (KeyValuePair<int, List<(Message, byte[])>> kvp in UnParsedMessages)
                {
                    Console.WriteLine(kvp.Key + ": " + kvp.Value.Count);
                }
                Console.WriteLine("Connections: ");
                foreach (KeyValuePair<int, Connection> kvp in rosbag.Connections)
                {
                    Console.WriteLine(kvp.Key);
                }
                Console.WriteLine("Messages ");
                foreach (KeyValuePair<int, List<Message>> kvp in rosbag.Messages)
                {
                    Console.WriteLine(kvp.Key + ": " + kvp.Value.Count);
                }
                throw new Exception("There are messages without the corresponding connection");
            }
        }
    }
}
