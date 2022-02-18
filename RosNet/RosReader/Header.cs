using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RosNet.Field;
using RosNet.Type;

namespace RosNet.RosReader
{
    /// <summary>
    /// Helper class for reading headers of records
    /// </summary>
    public static class Header
    {
        //Dictionary containing the names of all header fields in record
        internal static Dictionary<int, string[]> HeaderFieldsByOp = new Dictionary<int, string[]>()
        {
            { 2 , new string[] {"conn", "time"} },
            { 3 , new string[] {"index_pos", "conn_count", "chunk_count"} },
            { 4 , new string[] {"ver", "conn", "count"} },
            { 5 , new string[] {"compression", "size"} },
            { 6 , new string[] {"ver", "chunk_pos", "start_time", "end_time", "count"} },
            { 7 , new string[] {"conn", "topic"} }
        };

        /// <summary>
        /// Reads a header
        /// </summary>
        /// <returns>Dictionary with fieldnames and fieldvalues in header</returns>
        internal static Dictionary<string, FieldValue> ReadHeader(BinaryReader reader)
        {
            int headerLen = reader.ReadInt32();
            var headerFields = new Dictionary<string, FieldValue>();

            //checks if all fields in header are read
            bool hasAllFields = false;
            while (!hasAllFields)
            {
                FieldValue fieldValue = Header.ReadField(reader);
                headerFields.Add(fieldValue.Name, fieldValue);

                if (headerFields.ContainsKey("op"))
                {
                    hasAllFields = true;
                    foreach (string headerField in HeaderFieldsByOp[(int)headerFields["op"].Value.First()])
                    {
                        if (!headerFields.ContainsKey(headerField))
                        {
                            hasAllFields = false;
                        }
                    }
                }
            }
            return headerFields;
        }

        /// <summary>
        /// Reads a field
        /// </summary>
        /// <returns>FieldValue containing name, datatype and value of field</returns>
        internal static FieldValue ReadField(BinaryReader reader)
        {
            int fieldLen = reader.ReadInt32();
            string fieldName = ReadName(reader);

            PrimitiveType dataType;
            byte[] fieldValue;
            switch (fieldName)
            {
                case "index_pos":
                case "time":
                case "chunk_pos":
                case "start_time":
                case "end_time":
                    dataType = PrimitiveType.Int64;
                    fieldValue = reader.ReadBytes(8);
                    break;
                case "conn_count":
                case "chunk_count":
                case "size":
                case "conn":
                case "ver":
                case "count":
                case "offset":
                    dataType = PrimitiveType.Int32;
                    fieldValue = reader.ReadBytes(4);
                    break;
                case "op":
                    dataType = PrimitiveType.Int8;
                    fieldValue = reader.ReadBytes(1);
                    break;
                case "compression":
                    dataType = PrimitiveType.String;
                    char firstChar = reader.ReadChar();
                    if (firstChar.Equals('n'))
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
                    dataType = PrimitiveType.String;
                    fieldValue = Encoding.ASCII.GetBytes(new string(reader.ReadChars(fieldLen - 6)));
                    break;
                default:
                    throw new Exception($"{fieldName} not defined in ROSbag-format");
            }
            return new FieldValue(fieldName, dataType, fieldValue);
        }

        /// <summary>
        /// Reads a field name
        /// </summary>
        /// <returns> name of field</returns>
        internal static string ReadName(BinaryReader reader)
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
    }
}
