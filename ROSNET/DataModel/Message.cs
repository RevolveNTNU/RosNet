using System;
using System.Collections.Generic;
using ROSNET.Field;

namespace ROSNET.DataModel
{
    /// <summary>
    /// Represents a ROSbag message
    /// </summary>
    public class Message
    {
        //Header fields of message record:
        public int Conn { get; }
        public long Time { get;  }

        //Data in message record:
        public Dictionary<string,FieldValue> Data { get; set; }

        /// <summary>
        /// Create a message with conn and time from message record header
        /// </summary>
        public Message(FieldValue conn, FieldValue time)
        {
            this.Conn = BitConverter.ToInt32(conn.Value);
            this.Time = BitConverter.ToInt64(time.Value);
        }

        public override string ToString()
        {
            var s = "Conn: " + Conn + "\n";
            s += "Time: " + Time + "\n";
            s += "Data: " + "\n";
            foreach (KeyValuePair<string, FieldValue> kvp in Data)
            {
                s += kvp.Value.ToString(true) + "\n";
            }

            return s;
        }
    }
}
