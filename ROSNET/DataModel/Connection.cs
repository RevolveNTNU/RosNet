using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using ROSNET;

namespace ROSbag_ReadWrite.DataModel
{
    public class Connection
    {
        public int Conn { get; }
        public string Topic { get; }
        public string OriginalTopic { get; private set; }
        public string Type { get; private set; }
        public string Md5sum { get; private set; }
        public string MessageDefinition { get; private set;  }
        public string CallerID { get; private set; }
        public string Latching { get; private set; }

        public Connection(int conn, string topic, BinaryReader reader)
        {
            Conn = conn;
            Topic = topic;
            SetData(reader);
        }

        private void SetData(BinaryReader reader)
        {
            long endPos = reader.BaseStream.Position + reader.ReadInt32();
            int fieldLen;
            string fieldName;
            string fieldValue;
            while (reader.BaseStream.Position != endPos)
            {
                fieldLen = reader.ReadInt32();
                fieldName = Header.ReadName(reader, fieldLen);
                fieldValue = System.Text.Encoding.Default.GetString(reader.ReadBytes(fieldLen - fieldName.Length - 1));

                switch (fieldName)
                {
                    case "topic":
                        OriginalTopic = fieldValue;
                        break;
                    case "type":
                        Type = fieldValue;
                        break;
                    case "md5sum":
                        Md5sum = fieldValue;
                        break;
                    case "message_definition":
                        MessageDefinition = fieldValue;
                        break;
                    case "callerid":
                        CallerID = fieldValue;
                        break;
                    case "latching":
                        Latching = fieldValue;
                        break;
                }
            }
        }
    }
}
