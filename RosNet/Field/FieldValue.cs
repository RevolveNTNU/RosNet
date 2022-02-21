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
                case PrimitiveType.Bool:
                case PrimitiveType.Int8:
                case PrimitiveType.Uint8:
                case PrimitiveType.Byte:
                case PrimitiveType.Char:
                    return 1;
                case PrimitiveType.Int16:
                case PrimitiveType.Uint16:
                    return 2;
                case PrimitiveType.Int32:
                case PrimitiveType.Uint32:
                case PrimitiveType.Float32:
                case PrimitiveType.String:
                    return 4;
                case PrimitiveType.Int64:
                case PrimitiveType.Uint64:
                case PrimitiveType.Float64:
                case PrimitiveType.Time:
                case PrimitiveType.Duration:
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
        public string PrettyValue()
        {
            switch (this.DataType)
            {
                case PrimitiveType.Bool:
                    return ($"{BitConverter.ToBoolean(this.Value)}");
                case PrimitiveType.Int8:
                    return ((int)this.Value.First()).ToString();
                case PrimitiveType.Uint8:
                    return ((uint)this.Value.First()).ToString();
                case PrimitiveType.Byte:
                    return ($"{(sbyte) this.Value.First()}");
                case PrimitiveType.Char:
                    return ((char)this.Value.First()).ToString();
                case PrimitiveType.Int16:
                    return ($"{BitConverter.ToInt16(this.Value)}");
                case PrimitiveType.Uint16:
                    return ($"{BitConverter.ToUInt16(this.Value)}");
                case PrimitiveType.Int32:
                    return ($"{BitConverter.ToInt32(this.Value)}");
                case PrimitiveType.Uint32:
                    return ($"{BitConverter.ToUInt32(this.Value)}");
                case PrimitiveType.Int64:
                    return ($"{BitConverter.ToInt64(this.Value)}");
                case PrimitiveType.Uint64:
                    return ($"{BitConverter.ToUInt64(this.Value)}");
                case PrimitiveType.Float32:
                    return ($"{BitConverter.ToSingle(this.Value)}");
                case PrimitiveType.Float64:
                    return ($"{BitConverter.ToDouble(this.Value)}");
                case PrimitiveType.String:
                    return System.Text.Encoding.Default.GetString(this.Value); ;
                case PrimitiveType.Time:
                    uint timeSecs = BitConverter.ToUInt32(this.Value.Take(4).ToArray());
                    uint timeNsecs = BitConverter.ToUInt32(this.Value.Skip(4).Take(4).ToArray());
                    return ($"{timeSecs} : {timeNsecs}");
                case PrimitiveType.Duration:
                    int durationSecs = BitConverter.ToInt32(this.Value.Take(4).ToArray());
                    int durationNsecs = BitConverter.ToInt32(this.Value.Skip(4).Take(4).ToArray());
                    return ($"{durationSecs} : {durationNsecs}");
                default:
                    //TODO exception
                    return "";
            }
        }
    }
}
