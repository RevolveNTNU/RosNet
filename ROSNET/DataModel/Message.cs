using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ROSNET.DataModel
{
    public class Message
    {
        public int Conn { get; }
        public long Time { get;  }
        private Dictionary<string,FieldValue> Data { get; }

        public Message(FieldValue conn, FieldValue time)
        {
            Conn = BitConverter.ToInt32(conn.Value);
            Time = BitConverter.ToInt64(time.Value);
            Data = new Dictionary<string, FieldValue>();
        }

        public Message(FieldValue conn, FieldValue time, BinaryReader reader, List<FieldValue> messageDefinition)
        {
            Conn = BitConverter.ToInt32(conn.Value);
            Time = BitConverter.ToInt64(time.Value);
            ReadData(reader, messageDefinition);
        }

        private void ReadData(BinaryReader reader, List<FieldValue> messageDefinition)
        {
            int dataLength = reader.ReadInt32();
            reader.ReadBytes(dataLength);
        }

        public void SetData(byte[] data, List<FieldValue> messageDefinition)
        {
            //les gjennom meldingen
        }

        public string toString()
        {
            string s = "Conn: " + Conn + "\n";
            s += "Time: " + Time + "\n";
            s += "Data: " + "\n";
            foreach (KeyValuePair<string, FieldValue> kvp in Data)
            {
                s += kvp.Value.toString();
            }

            return s;
        }
    }
}
