using System;
using System.IO;
using System.Collections.Generic;

namespace ROSNET
{
    public static class DataReader
    {


        public static string ReadData(BinaryReader reader, int dataLen, string op)
        {
            switch(op)
            {
                case "5":
                    return DataReader.ReadChunk(reader, dataLen);
                case "2":
                    //return System.Text.Encoding.Default.GetString(reader.ReadBytes(dataLen));
                    reader.ReadBytes(dataLen);
                    return $"DATA OF {dataLen} BYTES HERE";
                case "7":
                    return new string(reader.ReadChars(dataLen));
                default:
                    reader.ReadBytes(dataLen);
                    return $"";
            }
        }

        public static string ReadChunk(BinaryReader reader, int dataLen)
        {
            int read = 0;
            while (read < dataLen)
            {
                int headerLen = reader.ReadInt32();
                read += 4;
                //Console.WriteLine($"    Header len: {headerLen}");
                int fieldLen;
                Dictionary<string, string> fields = new Dictionary<string, string>();
                string fieldName;
                string fieldValue;
                bool hasReqs = false;
                while (!hasReqs)
                {
                    fieldLen = reader.ReadInt32();
                    read += 4;
                    fieldName = Header.ReadName(reader);
                    fieldValue = Header.ReadFieldValue(reader, fieldName, fieldLen);
                    read += fieldLen;
                    fields.Add(fieldName, fieldValue);

                    if (fields.ContainsKey("op"))
                    {
                        hasReqs = true;
                        //foreach (string req in Header.OpReqs[fields["op"]])
                        {
                            //if (!fields.ContainsKey(req)) hasReqs = false;
                        }
                    }

                    /*if (fieldName == "op")
                    {
                        switch (fieldValue)
                        {
                            case "2":
                                Console.WriteLine("    Message Data");
                                break;
                            case "7":
                                Console.WriteLine("    Connection");
                                break;
                            default:
                                Console.WriteLine($"Wrong record type within chunk. Got type {fieldValue}");
                                break;
                        }
                    }*/
                    //Console.WriteLine($"{fieldName} : {fieldValue}");
                }
                int dataLen2 = reader.ReadInt32();
                read += 4;
                //Console.WriteLine($"Data len: {dataLen2}");
                string data = ReadData(reader, dataLen2, fields["op"]);
                read += dataLen2;
            }

            return "";
        }
    }
}
