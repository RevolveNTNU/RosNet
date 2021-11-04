using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ROSbag_ReadWrite
{
    public static class Reader
    {
        public static void Read()
        {
            string currentDir = Directory.GetCurrentDirectory();
            string regexPath = Regex.Replace(currentDir, "ROSbag-ReadWrite.*", "ROSbag-ReadWrite/bags/acc19-11.bag");

            if (File.Exists(regexPath))
            {
                using (BinaryReader reader = new BinaryReader(File.Open(regexPath, FileMode.Open)))
                {
                    Console.Write(reader.ReadChars(13));

                    while (reader.BaseStream.Position != reader.BaseStream.Length)
                    {
                        int headerLen = reader.ReadInt32();
                        int fieldLen;
                        Dictionary<string, string> fields = new Dictionary<string, string>();
                        string fieldName;
                        string fieldValue;
                        bool hasReqs = false;
                        while (!hasReqs)
                        {
                            fieldLen = reader.ReadInt32();
                            fieldName = Header.ReadName(reader, fieldLen);
                            fieldValue = Header.ReadValue(reader, fieldName, fieldLen);
                            fields.Add(fieldName, fieldValue);

                            if (fields.ContainsKey("op"))
                            {
                                hasReqs = true;
                                foreach (string req in Header.OpReqs[fields["op"]])
                                {
                                    if (!fields.ContainsKey(req)) hasReqs = false;
                                }
                            }

                            Console.WriteLine($"{fieldName} : {fieldValue}");
                        }
                        int dataLen = reader.ReadInt32();
                        byte[] data = reader.ReadBytes(dataLen);
                        Console.WriteLine($"\nDATA HERE OF {dataLen} BYTES\n");
                    }
                }
            }
        }
    }
}
