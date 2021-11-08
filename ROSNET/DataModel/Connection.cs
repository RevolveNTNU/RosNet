using System;
using System.Collections.Generic;
using System.Text;

namespace ROSbag_ReadWrite.DataModel
{
    public class Connection
    {
        public int Conn { get; }
        public string Topic { get; }
        public string OriginalTopic { get; }
        public string Type { get; }
        public string Md5sum { get; }
        public string MessageDefinition { get; }
        public string CallerID { get; }
        public string Latching { get; }

        public Connection(int conn, string topic, byte[] data)
        {
            Conn = conn;
            Topic = topic;
        }
    }
}
