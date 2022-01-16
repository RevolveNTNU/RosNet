using System;
using System.Collections.Generic;
using ROSNET;
using ROSNET.Enum;

namespace ROSbag_ReadWrite
{
    public class ArrayFieldValue : FieldValue
    {
        public List<FieldValue> Definition { get; set; }
        public int Size { get; set; }

        public ArrayFieldValue(string name, List<FieldValue> definition):base(name, PrimitiveType.ARRAY)
        {
            this.Definition = definition;
        }

        public ArrayFieldValue(string name, List<FieldValue> definition, int size) : base(name, PrimitiveType.ARRAY)
        {
            this.Definition = definition;
            this.Size = size;
        }
    }
}
