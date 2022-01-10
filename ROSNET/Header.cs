using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Linq;

namespace ROSNET
{
    public static class Header
    {
        public static Dictionary<int, string[]> OpReqs = new Dictionary<int, string[]>()
        {
            { 2 , new string[] {"conn", "time"} },
            { 3 , new string[] {"index_pos", "conn_count", "chunk_count"} },
            { 4 , new string[] {"ver", "conn", "count"} },
            { 5 , new string[] {"compression", "size"} },
            { 6 , new string[] {"ver", "chunk_pos", "start_time", "end_time", "count"} },
            { 7 , new string[] {"conn", "topic"} }
        };

        public static Dictionary<string, FieldValue> readHeader(BinaryReader reader)
        {
            int headerLen = reader.ReadInt32();
            Dictionary<string, FieldValue> fields = new Dictionary<string, FieldValue>();
            FieldValue fieldValue;
            bool hasReqs = false;
            while (!hasReqs)
            {
                fieldValue = Header.ReadField(reader);
                fields.Add(fieldValue.Name, fieldValue);
                if (fields.ContainsKey("op"))
                {
                    hasReqs = true;
                    foreach (string req in OpReqs[Convert.ToInt32(fields["op"].Value[fields["op"].Value.Length - 1])])
                    {
                        if (!fields.ContainsKey(req)) hasReqs = false;
                    }

                }
                //Console.WriteLine($"{fieldName} : {fieldValue}");
            }
            return fields;
        }

        private static FieldValue ReadField(BinaryReader reader)
        {
            int fieldLen = reader.ReadInt32();
            string fieldName = ReadName(reader);

            String dataType;
            byte[] fieldValue;
            switch (fieldName)
            {
                case "index_pos":
                case "time":
                case "chunk_pos":
                case "start_time":
                case "end_time":
                    dataType = "long";
                    fieldValue = reader.ReadBytes(8);
                    break;
                case "conn_count":
                case "chunk_count":
                case "size":
                case "conn":
                case "ver":
                case "count":
                case "offset":
                    dataType = "int";
                    fieldValue = reader.ReadBytes(4);
                    break;
                case "op":
                    dataType = "byte";
                    fieldValue = reader.ReadBytes(1);
                    break;
                case "compression":
                    dataType = "string";
                    string firstChar = reader.ReadChar().ToString();
                    if (firstChar.Equals("n"))
                    {
                        reader.ReadChars(3);
                        fieldValue = Encoding.ASCII.GetBytes("none");
                    }
                    else
                    {
                        reader.ReadChars(2);
                        fieldValue = Encoding.ASCII.GetBytes("bz2");
                    }
                    break;
                case "topic":
                    dataType = "string";
                    fieldValue = Encoding.ASCII.GetBytes(new string(reader.ReadChars(fieldLen - 6)));
                    break;
                default:
                    //TODO: make own exceptions
                    throw new Exception($"{fieldName} not defined in ROSbag-format");
            };
            return new FieldValue(fieldName, dataType, fieldValue);
        }

        public static string ReadName(BinaryReader reader)
        {
            char curChar;
            string fieldName = "";
            do
            {
                curChar = reader.ReadChar();
                if (curChar != '=')
                {
                    fieldName += curChar;
                }
            }
            while (curChar != '=');
            
            return fieldName;
        }


        //UNUSED

        public static string ReadFieldValue(BinaryReader reader, string fieldName, int fieldLen)
        {
            string fieldValue = "";
            switch (fieldName)
            {
                case "index_pos":
                case "time":
                case "chunk_pos":
                case "start_time":
                case "end_time":
                    fieldValue = reader.ReadInt64().ToString();
                    break;
                case "conn_count":
                case "chunk_count":
                case "size":
                case "conn":
                case "ver":
                case "count":
                case "offset":
                    fieldValue = reader.ReadInt32().ToString();
                    break;
                case "op":
                    fieldValue = reader.ReadByte().ToString();
                    break;
                case "compression":
                    fieldValue = reader.ReadChar().ToString();
                    if (fieldValue == "n")
                    {
                        reader.ReadChars(3);
                        fieldValue = "none";
                    }
                    else
                    {
                        reader.ReadChars(2);
                        fieldValue = "bz2";
                    }
                    break;
                case "topic":
                    fieldValue = new string(reader.ReadChars(fieldLen - 6));
                    break;
            }
            return fieldValue;
        }
    }
}
