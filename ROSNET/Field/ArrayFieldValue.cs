using System.Collections.Generic;
using System.Linq;
using ROSNET.Enum;

namespace ROSNET.Field
{
    public class ArrayFieldValue : FieldValue
    {
        public List<FieldValue> ArrayFields { get; set; }
        public int FixedArrayLength { get;}

        public ArrayFieldValue(string name, List<FieldValue> definition):base(name, definition.First().DataType)
        {
            this.ArrayFields = definition;

        }

        public ArrayFieldValue(string name, List<FieldValue> definition, int fixedArrayLength) : base(name, definition.First().DataType)
        {
            this.ArrayFields = definition;
            this.FixedArrayLength = fixedArrayLength;
        }

        //todo
        public override int GetBitLength()
        {
            return 0;
        }
    }
