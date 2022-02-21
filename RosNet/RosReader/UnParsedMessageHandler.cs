using System;
using System.Collections.Generic;
using RosNet.DataModel;
using RosNet.RosMessageParser;

namespace RosNet.RosReader
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
        internal void AddUnParsedMessage(Message message, byte[] data)
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
        internal void ParseMessages(RosBag rosBag)
        {
            int i = 0;
            foreach (KeyValuePair<int, Connection> kvp in rosBag.Connections)
            {
                
                foreach((var message, var data) in UnParsedMessageByConn[kvp.Key])
                {
                    message.Data = MessageDataParser.ParseMessageData(data, kvp.Value.MessageDefinition);
                    rosBag.Connections[message.Conn].Messages.Add(message);
                    i++;
                }
                UnParsedMessageByConn.Remove(kvp.Key);
                i = 0;
            }

            if (UnParsedMessageByConn.Count != 0)
            {
                throw new Exception("There are messages without the corresponding connection");
            }
        }
    }
}
