using System;
using System.Collections.Generic;
using ROSNET.DataModel;

namespace ROSNET.ROSReader
{
    /// <summary>
    /// Contains message-records with unparsed data because their corresponding connections are not read yet
    /// </summary>
    public class UnReadMessageHandler
    {
        public ROSbag Rosbag { get; private set; }
        public Dictionary<int, List<(Message, byte[])>> UnReadMessages { get; private set; }

        public UnReadMessageHandler(ROSbag Rosbag)
        {
            this.Rosbag = Rosbag;
            UnReadMessages = new Dictionary<int, List<(Message, byte[])>>();
        }

        public void AddUnReadMessage(Message message, byte[] data)
        {
            if (UnReadMessages.ContainsKey(message.Conn))
            {
                UnReadMessages[message.Conn].Add((message, data));
            }
            else
            {
                UnReadMessages.Add(message.Conn, new List<(Message, byte[])>() { (message, data) });
            }
        }

        public void UpdateUnReadMessages(Connection connection)
        {
            if (UnReadMessages.ContainsKey(connection.Conn))
            {
                foreach ((var message, var data) in UnReadMessages[connection.Conn])
                {
                    message.SetData(data, connection.MessageDefinition);
                    Rosbag.AddMessage(message);
                }
            }
            UnReadMessages.Remove(connection.Conn);
        }
    }
}
