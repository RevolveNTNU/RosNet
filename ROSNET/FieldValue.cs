using System;
using System.Collections;
using ROSNET.Enum;

namespace ROSNET
{
    public class FieldValue
    {

        public string Name { get; private set; }
        public PrimitiveType DataType { get; private set; }
        public byte[] Value { get; set; }

        public FieldValue(string Name, PrimitiveType DataType, byte[] Value)
        {
            this.Name = Name;
            this.DataType = DataType;
            this.Value = Value;
        }

        public FieldValue(string Name, PrimitiveType DataType)
        {
            this.Name = Name;
            this.DataType = DataType;
        }


        public int GetBitLength()
        {
            switch(DataType)
            {
                case PrimitiveType.BOOL:
                    return 1;
                case PrimitiveType.INT8:
                case PrimitiveType.UINT8:
                case PrimitiveType.BYTE:
                case PrimitiveType.CHAR:
                    return 8;
                case PrimitiveType.INT16:
                case PrimitiveType.UNIT16:
                    return 16;
                case PrimitiveType.INT32:
                case PrimitiveType.UINT32:
                case PrimitiveType.FLOAT32:
                case PrimitiveType.STRING:
                    return 32;
                case PrimitiveType.INT64:
                case PrimitiveType.FLOAT64:
                case PrimitiveType.TIME:
                case PrimitiveType.DURATION:
                    return 64;
                case PrimitiveType.ARRAY:
                    throw new Exception("FieldValue is not an array");
                default:
                    return 0;
            }
       

        }

        public string toString()
        {
            return ($"{DataType} {Name} {Value}");

        }

    }
}
