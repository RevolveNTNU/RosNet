using System;
using ROSNET.Type;

namespace ROSNET.Field
{
    public class FieldValue
    {

        public string Name { get; set; }
        public PrimitiveType DataType { get; private set; }
        public byte[] Value { get; private set; }

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



        public virtual int GetByteLength()
        {
            switch (this.DataType)
            {
                case PrimitiveType.BOOL:
                case PrimitiveType.INT8:
                case PrimitiveType.UINT8:
                case PrimitiveType.BYTE:
                case PrimitiveType.CHAR:
                    return 1;
                case PrimitiveType.INT16:
                case PrimitiveType.UINT16:
                    return 2;
                case PrimitiveType.INT32:
                case PrimitiveType.UINT32:
                case PrimitiveType.FLOAT32:
                case PrimitiveType.STRING:
                    return 4;
                case PrimitiveType.INT64:
                case PrimitiveType.FLOAT64:
                case PrimitiveType.TIME:
                case PrimitiveType.DURATION:
                    return 8;
                default:
                    return 0;
            }
        }

        public virtual string ToString(bool printValue)
        {
            if (printValue)
            {
                return this.ToString(); 
            }
            return ($"{DataType} {Name}");

        }

        public override string ToString()
        {
            if (this.Value.Length == 0)
            {
                return ($"{DataType} {Name} noValue");
            }
            return ($"{DataType} {Name} {PrettyValue()}");

        }

        //todo: this doesnt work well
        public string PrettyValue()
        {
            switch (this.DataType)
            {
                case PrimitiveType.BOOL:
                    return ("" + BitConverter.ToBoolean(this.Value));
                case PrimitiveType.CHAR:
                    return ("" + BitConverter.ToChar(this.Value));
                case PrimitiveType.INT8:
                    return ("" + (sbyte)BitConverter.ToChar(this.Value));
                case PrimitiveType.INT16:
                case PrimitiveType.BYTE:
                    return ("" + BitConverter.ToInt16(this.Value));
                case PrimitiveType.UINT8:
                case PrimitiveType.UINT16:
                    return ("" + BitConverter.ToUInt16(this.Value));
                case PrimitiveType.INT32:
                case PrimitiveType.FLOAT32:
                case PrimitiveType.STRING:
                    return ("" + BitConverter.ToInt32(this.Value));
                case PrimitiveType.UINT32:
                    return ("" + BitConverter.ToUInt32(this.Value));
                case PrimitiveType.INT64:
                case PrimitiveType.FLOAT64:
                case PrimitiveType.TIME:
                case PrimitiveType.DURATION:
                    return ("" + BitConverter.ToInt64(this.Value));
                default:
                    //TODO exception

                    return "";
            }
        }

    }
}
