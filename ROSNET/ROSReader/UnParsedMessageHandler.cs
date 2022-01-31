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
        public Dictionary<int, List<(Message, byte[])>> UnParsedMessageByConn { get; private set; }

        /// <summary>
        /// Create an UnParsedMessageHandler with an empty dictionary of unparsed messages
        /// </summary>
        public UnParsedMessageHandler()
        {
            UnParsedMessageByConn = new Dictionary<int, List<(Message, byte[])>>();
        }

        /// <summary>
        /// Adds a Message object and data to dictionary of unparsed messages
        /// </summary>
        public void AddUnParsedMessage(Message message, byte[] data)
        {
            if (UnParsedMessageByConn.ContainsKey(message.Conn))
            {
                UnParsedMessageByConn[message.Conn].Add((message, data));
            }
            else
            {
                UnParsedMessageByConn.Add(message.Conn, new List<(Message, byte[])>() { (message, data) });
            }
        }

        /// <summary>
        /// Parses data of all messages in list of unparsed messages
        /// </summary>
        public void ParseMessages(ROSbag rosbag)
        {
            foreach(KeyValuePair<int, Connection> kvp in rosbag.Connections)
            {
                foreach((var message, var data) in UnParsedMessageByConn[kvp.Key])
                {
                    message.Data = MessageDataParser.ParseMessageData(data, kvp.Value.MessageDefinition);
                    rosbag.Connections[message.Conn].AddMessage(message);
                }
                UnParsedMessageByConn.Remove(kvp.Key);
            }

            if (UnParsedMessageByConn.Count != 0)
            {
                Console.WriteLine("UnReadMessages: ");
                foreach (KeyValuePair<int, List<(Message, byte[])>> kvp in UnParsedMessageByConn)
                {
                    Console.WriteLine(kvp.Key + ": " + kvp.Value.Count);
                }
                Console.WriteLine("Connections: ");
                foreach (KeyValuePair<int, Connection> kvp in rosbag.Connections)
                {
                    Console.WriteLine(kvp.Key + ":" + kvp.Value.Messages.Count);
                }
                throw new Exception("There are messages without the corresponding connection");
            }
        }
    }
}
