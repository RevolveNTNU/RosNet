using System.Collections.Generic;
using System.Linq;
using ROSNET.Type;

namespace ROSNET.Field
{
    public class ArrayFieldValue : FieldValue
    {
        public List<FieldValue> ArrayFields { get; set; }
        public uint FixedArrayLength { get; }

        public ArrayFieldValue(string name, List<FieldValue> definition) : base(name, PrimitiveType.ARRAY)
        {
            this.ArrayFields = definition;

        }

        public ArrayFieldValue(string name, List<FieldValue> definition, uint fixedArrayLength) : base(name, PrimitiveType.ARRAY)
        {
            this.ArrayFields = definition;
            this.FixedArrayLength = fixedArrayLength;
        }

        //todo 
        public override int GetByteLength()
        {
            return 0;
        }

        /// <summary>
        /// Creates string with values if printValue is true, else string without values
        /// </summary>
        public override string ToString(bool printValue)
        {
            if (printValue)
            {
                return this.ToString();
            }
            return ($"{DataType}[] {Name}");

        }

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
