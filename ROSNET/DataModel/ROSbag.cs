using System;
using System.Collections.Generic;
using System.IO;

namespace ROSNET.DataModel
{
    public class ROSbag
    {
        private BinaryReader Reader { get; set; }
        public Dictionary<int, Connection> Connections { get; private set; }
        public Dictionary<int, List<Message>> Messages { get; private set; }
        public Dictionary<int, List<Tuple<Message, byte[]>>> UnReadMessages { get; private set; }

        public ROSbag()
        {
            Connections = new Dictionary<int, Connection>();
            Messages = new Dictionary<int, List<Message>>();
            UnReadMessages = new Dictionary<int, List<Tuple<Message, byte[]>>>();
        }

        public ROSbag(string path)
        {
            Reader = new BinaryReader(File.Open(path, FileMode.Open));
        }

        /// <summary>
        /// Adds a Connection object to the ROSbag's list of connections
        /// </summary>
        /// <returns>true if connection was successfully added</returns>
        public bool AddConnection(Connection conn)
        {
            if (!Connections.ContainsKey(conn.Conn))
            {
                Connections.Add(conn.Conn, conn);
                updateUnReadMessages(conn);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Adds a Message object to the ROSbag's list of messages
        /// </summary>
        public void AddMessage(Message message)
        {
            //nødvendig å sjekke om det er der?
            if (Messages.ContainsKey(message.Conn))
            {
                Messages[message.Conn].Add(message);
            } else
            {
                Messages.Add(message.Conn, new List<Message>() {message});
            }
        }

        /// <summary>
        /// Adds an UnReadMessage object to the ROSbag's list of UnReadMessages
        /// </summary>
        public void AddUnReadMessage(Message message, byte[] data)
        {
            if (UnReadMessages.ContainsKey(message.Conn))
            {
                UnReadMessages[message.Conn].Add(new Tuple<Message, byte[]>(message, data));
            }
            else
            {
                UnReadMessages.Add(message.Conn, new List<Tuple<Message, byte[]>>() { new Tuple<Message, byte[]>(message, data) });
            }
        }

        private void updateUnReadMessages(Connection connection)
        {
            if (UnReadMessages.ContainsKey(connection.Conn))
            {
                foreach (Tuple<Message, byte[]> unReadMessage in UnReadMessages[connection.Conn])
                {
                    Console.WriteLine($"Sets data of Message with conn: {unReadMessage.Item1.Conn}");
                    unReadMessage.Item1.SetData(unReadMessage.Item2, connection.MessageDefinition);
                    AddMessage(unReadMessage.Item1);
                }
            }
            UnReadMessages.Remove(connection.Conn);
        }

        public string toString()
        {
            string s = "ROSbag \n";
            s += "Connections:";
            foreach (KeyValuePair<int, Connection> kvp in Connections)
            {
                s += "Connection: " + kvp.Key + "\n";
                s += kvp.Value.toString();

                s += "Messages connected to this connection: \n";

                foreach(Message message in Messages.GetValueOrDefault(kvp.Key))
                {
                    s += message.ToString();
                }
            }
            return s;
        }


    }
}