using System;
using System.Collections.Generic;
using System.IO;
using ROSNET.Field;
using ROSNET.ROSMessageParser;

namespace ROSNET.DataModel
{
    public class Message
    {
        public int Conn { get; }
        public long Time { get;  }
        public Dictionary<string,FieldValue> Data { get; private set; }

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
            var dataLength = reader.ReadInt32();
            var data = reader.ReadBytes(dataLength);
            SetData(data, messageDefinition);
        }

        public void SetData(byte[] data, List<FieldValue> messageDefinition)
        {

            this.Data = MessageDataParser.ParseMessageData(data, messageDefinition);

        }

        public string toString()
        {
            var s = "Conn: " + Conn + "\n";
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
