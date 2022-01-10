using System;
using System.Collections;

namespace ROSNET
{
    public class FieldValue
    {

        public string Name { get; private set; }
        public string DataType { get; private set; }
        public byte[] Value { get; set; }
        public int bitLength;

        public FieldValue(string Name, string DataType, byte[] Value)
        {
            this.Name = Name;
            this.DataType = DataType;
            this.Value = Value;
        }

        public FieldValue(string Name, string DataType)
        {
            this.Name = Name;
            this.DataType = DataType;
        }


        //TODO
        public int getBitLength()
        {
            return 0;
       

        }

        public string toString()
        {
            return ($"{DataType} {Name} {Value}");

        }

    }
}
