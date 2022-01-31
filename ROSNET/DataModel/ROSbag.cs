using System.Collections.Generic;

namespace ROSNET.DataModel
{
    /// <summary>
    /// Represents a ROSbag
    /// </summary>
    public class ROSbag
    {
        public Dictionary<int, Connection> Connections { get; private set; } //List of connection records in rosbag

        /// <summary>
        /// Creates an empty rosbag
        /// </summary>
        public ROSbag()
        {
            Connections = new Dictionary<int, Connection>();
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
        

        public override string ToString()
        {
            string s = "ROSbag \n";
            s += "Connections:";
            foreach (KeyValuePair<int, Connection> kvp in Connections)
            {
                s += "Connection: " + kvp.Key + "\n";
                s += kvp.Value.ToString();
            }
            return s;
        }


    }
}