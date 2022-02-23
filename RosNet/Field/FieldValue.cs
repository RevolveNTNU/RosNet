using System;
using System.Linq;
using System.Text;
using RosNet.Type;

namespace RosNet.Field
{
    /// <summary>
    /// Represents a fieldvalue
    /// </summary>
    public class FieldValue : IFieldValue
    {
        public string Name { get; set; }
        public PrimitiveType DataType { get; private set; }
        public byte[] Value { get; private set; }

        /// <summary>
        /// Creates a fieldvalue with name, datatype and value
        /// </summary>
        public FieldValue(string Name, PrimitiveType DataType, byte[] Value)
        {
            this.Name = Name;
            this.DataType = DataType;
            this.Value = Value;
        }

        /// <summary>
        /// Creates a fieldvalue with name and datatype
        /// </summary>
        public FieldValue(string Name, PrimitiveType DataType)
        {
            this.Name = Name;
            this.DataType = DataType;
        }

        /// <summary>
        /// Finds the byte length of the fieldvalue using the datatype
        /// </summary>
        /// <returns>byte length of fieldvalue</returns>
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
                case PrimitiveType.UINT64:
                case PrimitiveType.FLOAT64:
                case PrimitiveType.TIME:
                case PrimitiveType.DURATION:
                    return 8;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Creates string with value
        /// </summary>
        /// <returns>String with value</returns>
        public override string ToString()
        {
            if (this.Value.Length == 0)
            {
                return ($"{DataType} {Name} noValue");
            }
            return ($"{DataType} {Name} {PrettyValue()}");
        }

        /// <summary>
        /// Creates string of Value using datatype
        /// </summary>
        /// <returns>String of Value</returns>
        public string PrettyValue() => this.DataType switch
        {
            PrimitiveType.BOOL => ($"{BitConverter.ToBoolean(this.Value)}"),
            PrimitiveType.INT8 => ((int)this.Value.First()).ToString(),
            PrimitiveType.UINT8 => ((uint)this.Value.First()).ToString(),
            PrimitiveType.BYTE => ($"{(sbyte)this.Value.First()}"),
            PrimitiveType.CHAR => ((char)this.Value.First()).ToString(),
            PrimitiveType.INT16 => ($"{BitConverter.ToInt16(this.Value)}"),
            PrimitiveType.UINT16 => ($"{BitConverter.ToUInt16(this.Value)}"),
            PrimitiveType.INT32 => ($"{BitConverter.ToInt32(this.Value)}"),
            PrimitiveType.UINT32 => ($"{BitConverter.ToUInt32(this.Value)}"),
            PrimitiveType.INT64 => ($"{BitConverter.ToInt64(this.Value)}"),
            PrimitiveType.UINT64 => ($"{BitConverter.ToUInt64(this.Value)}"),
            PrimitiveType.FLOAT32 => ($"{BitConverter.ToSingle(this.Value)}"),
            PrimitiveType.FLOAT64 => ($"{BitConverter.ToDouble(this.Value)}"),
            PrimitiveType.STRING => System.Text.Encoding.Default.GetString(this.Value),
            PrimitiveType.TIME => ($"{BitConverter.ToUInt32(this.Value.Take(4).ToArray())} : {BitConverter.ToUInt32(this.Value.Skip(4).Take(4).ToArray())}"),
            PrimitiveType.DURATION => ($"{BitConverter.ToInt32(this.Value.Take(4).ToArray())} : {BitConverter.ToInt32(this.Value.Skip(4).Take(4).ToArray())}"),
            _ => throw new Exception("Datatype is not a primitive type") //todo make exception

        };
    }
}
