using System.Collections.Generic;
using System.Linq;
using ROSNET.Type;

namespace ROSNET.Field
{
    /// <summary>
    /// Represents an arrayfield
    /// </summary>
    public class ArrayFieldValue : FieldValue
    {
        public List<FieldValue> ArrayFields { get; set; }
        public uint FixedArrayLength { get; } //used if array has fixed length

        /// <summary>
        /// Creates an arrayfieldvalue with name and list of fields in array. Sets datatype to ARRAY.
        /// </summary>
        public ArrayFieldValue(string name, List<FieldValue> arrayFields) : base(name, PrimitiveType.ARRAY)
        {
            this.ArrayFields = arrayFields;
        }

        /// <summary>
        /// Creates an arrayfieldvalue with name, list of fields in array and fixed length of array. Sets datatype to ARRAY.
        /// </summary>
        public ArrayFieldValue(string name, List<FieldValue> arrayFields, uint fixedArrayLength) : base(name, PrimitiveType.ARRAY)
        {
            this.ArrayFields = arrayFields;
            this.FixedArrayLength = fixedArrayLength;
        }

        //todo 
        public override int GetByteLength()
        {
            return 0;
        }

        /// <summary>
        /// Creates string with values if printValue is true, else string without values (used for messageDefinition)
        /// </summary>
        /// <returns> string with values if printValue is true, else returns string without values</returns>
        public override string ToString(bool printValue)
        {
            if (printValue)
            {
                return this.ToString();
            }
            return ($"{DataType}[] {Name}");

        }

        /// <summary>
        /// Creates string with values
        /// </summary>
        /// <returns>string with values</returns>
        public override string ToString ()
        {
            var s = ($"{DataType}[] {Name}: [");

            int i = 0;
            foreach (var fieldValue in ArrayFields)
            {
               if (!(i == 0))
               {
                  s += ",";
               }
               if (fieldValue.Value.Length == 0)
               {
                  s += "noValue";
               }
               else
               {
                  s += ($"{fieldValue.PrettyValue()}");
               }
            }
            i = 1;

            s += "]";

            return s;
        }

    }
}
