using System;
using System.IO;
using System.Collections.Generic;

namespace ROSbag_ReadWrite
{
    public static class DataReader
    {
        public static string ReadData(BinaryReader reader, int dataLen, string op)
        {
            switch(op)
            {
                case "7":
                    return new string(reader.ReadChars(dataLen));
                default:
                    reader.ReadBytes(dataLen);
                    return $"DATA OF {dataLen} BYTES HERE";
            }
        }
    }
}
