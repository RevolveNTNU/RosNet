using System;
using System.Collections.Generic;
using System.IO;
using ROSNET.Field;
using ROSNET.Reader;
using ROSNET.ROSMessageParser;

namespace ROSNET.DataModel
{
    /// <summary>
    /// Represents a ROSbag connection
    /// </summary>
    public class Connection
    {
        public int Conn { get; }
        public string Topic { get; }
        public string OriginalTopic { get; private set; }
        public string Type { get; private set; }
        public string Md5sum { get; private set; }
        public List<FieldValue> MessageDefinition { get; private set;  }
        public string CallerID { get; private set; }
        public string Latching { get; private set; }


        /// <summary>
        /// Create a connection from conn and topic from connection record header, as well as a BinaryReader in position to read the connection header
        /// </summary>
        public Connection(FieldValue conn, FieldValue topic, BinaryReader reader)
        {
            Conn = BitConverter.ToInt32(conn.Value);
            Topic = System.Text.Encoding.Default.GetString(topic.Value);
            SetData(reader);
        }


        /// <summary>
        /// Helper function to extract data from connection header
        /// </summary>
        private void SetData(BinaryReader reader)
        {
            int dataLen = reader.ReadInt32();
            long endPos = reader.BaseStream.Position + dataLen;
            int fieldLen;
            string fieldName;
            string fieldValue;
            while (reader.BaseStream.Position != endPos)
            {
                fieldLen = reader.ReadInt32();
                fieldName = Header.ReadName(reader);
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
                        MessageDefinition = SetMessageDefinition(fieldValue);
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

        private List<FieldValue> SetMessageDefinition(string fieldValue)
        {
            var messageDefinition = MessageDefinitionParser.ParseMessageDefinition(fieldValue);

            return messageDefinition;
        }

        public string toString()
        {
            string s = "Conn: " + Conn + "\n";
            s += "Topic: " + Topic + "\n";
            s += "OriginalTopic: " + Conn + "\n";
            s += "Type: " + Type + "\n";
            s += "Md5sum: " + Md5sum + "\n";
            s += "MessageDefinition: " + "\n";
            foreach (FieldValue dataField in MessageDefinition)
            {
                s += dataField.toString();
            }
            s += "CallerID: " + CallerID + "\n";
            s += "Latching: " + Latching + "\n";
            return s;
        }
    }
}