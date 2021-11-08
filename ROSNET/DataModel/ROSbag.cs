using System;
using System.IO;
using System.Collections.Generic;

namespace ROSNET.DataModel
{
    public class ROSbag
    {
        private BinaryReader Reader { get; set; }
        public Dictionary<int, Connection> Connections { get; private set; }

        public ROSbag()
        {

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
                return true;
            }
            return false;
        }
    }
}