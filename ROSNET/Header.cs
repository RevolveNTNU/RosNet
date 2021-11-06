using System;
using System.IO;
using System.Collections.Generic;

namespace ROSbag_ReadWrite
{
    public static class Header
    {
        public static Dictionary<string, string[]> OpReqs = new Dictionary<string, string[]>()
        {
            { "2" , new string[] {"conn", "time"} },
            { "3" , new string[] {"index_pos", "conn_count", "chunk_count"} },
            { "4" , new string[] {"ver", "conn", "count"} },
            { "5" , new string[] {"compression", "size"} },
            { "6" , new string[] {"ver", "chunk_pos", "start_time", "end_time", "count"} },
            { "7" , new string[] {"conn", "topic"} }
        };

        public static string ReadName(BinaryReader reader, int fieldLen)
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

        public static string ReadValue(BinaryReader reader, string fieldName, int fieldLen)
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
