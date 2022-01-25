using System;
using System.Collections.Generic;
using System.IO;

namespace ROSNET.DataModel
{
    public class ROSbag
    {
        public Dictionary<int, Connection> Connections { get; private set; }
        public Dictionary<int, List<Message>> Messages { get; private set; }


        public ROSbag()
        {
            Connections = new Dictionary<int, Connection>();
            Messages = new Dictionary<int, List<Message>>();
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
        

        public override string ToString()
        {
            string s = "ROSbag \n";
            s += "Connections:";
            foreach (KeyValuePair<int, Connection> kvp in Connections)
            {
                s += "Connection: " + kvp.Key + "\n";
                s += kvp.Value.ToString();

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