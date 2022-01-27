using System;
using System.Linq;
using ROSNET.Type;

namespace ROSNET.Field
{
    /// <summary>
    /// Represents a fieldvalue
    /// </summary>
    public class FieldValue
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
                case PrimitiveType.FLOAT64:
                case PrimitiveType.TIME:
                case PrimitiveType.DURATION:
                    return 8;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Creates string with values if printValue is true, else string without values (used for messageDefinition)
        /// </summary>
        /// <returns>String with value if printValue is true, else returns string without value</returns>
        public virtual string ToString(bool printValue)
        {
            if (printValue)
            {
                return this.ToString(); 
            }
            return ($"{DataType} {Name}");

        }

        /// <summary>
        /// Creates string with values
        /// </summary>
        /// <returns>String with values</returns>
        public override string ToString()
        {
            if (this.Value.Length == 0)
            {
                return ($"{DataType} {Name} noValue");
            }
            return ($"{DataType} {Name} {PrettyValue()}");

        }

        //todo: this doesnt work well
        /// <summary>
        /// Creates string of Value using datatype
        /// </summary>
        /// <returns>String of Value</returns>
        public string PrettyValue()
        {
            switch (this.DataType)
            {
                case PrimitiveType.BOOL:
                    return ("" + BitConverter.ToBoolean(this.Value));
                case PrimitiveType.CHAR:
                    return ((char)this.Value.First()).ToString();
                case PrimitiveType.INT8:
                    return ((int)this.Value.First()).ToString();
                case PrimitiveType.BYTE:
                    return ("" + (int) this.Value.First());
                case PrimitiveType.INT16:
                    return ("" + BitConverter.ToInt16(this.Value));
                case PrimitiveType.UINT8:
                    return ((uint)this.Value.First()).ToString();
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
