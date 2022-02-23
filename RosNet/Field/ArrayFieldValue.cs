using System.Collections.Generic;
using RosNet.Type;

namespace RosNet.Field;

/// <summary>
/// Represents an arrayfield
/// </summary>
public class ArrayFieldValue : FieldValue, IArrayFieldValue
{
    public List<FieldValue> ArrayFields { get; set; }
    public uint FixedArrayLength { get; } //used if array has fixed length

    /// <summary>
    /// Creates an arrayfieldvalue with name and list of fields in array. Sets datatype to ARRAY.
    /// </summary>
    public ArrayFieldValue(string name, List<FieldValue> arrayFields, PrimitiveType primitiveType) : base(name, primitiveType)
    {
        this.ArrayFields = arrayFields;
    }

    /// <summary>
    /// Creates an arrayfieldvalue with name, list of fields in array and fixed length of array. Sets datatype to ARRAY.
    /// </summary>
    public ArrayFieldValue(string name, List<FieldValue> arrayFields, PrimitiveType primitiveType, uint fixedArrayLength) : base(name, primitiveType)
    {
        this.ArrayFields = arrayFields;
        this.FixedArrayLength = fixedArrayLength;
    }

    /// <summary>
    /// Finds the byte length of the array using fields in array
    /// </summary>
    /// <returns>byte length of array</returns>
    public override int GetByteLength()
    {
        int length = 0;
        foreach(var arrayField in ArrayFields)
        {
            length += arrayField.GetByteLength();
        }
        return length;
    }

    /// <summary>
    /// Creates string with values
    /// </summary>
    /// <returns>string with values</returns>
    public override string ToString ()
    {
        string s;
        if (this.DataType == PrimitiveType.STRING)
        {
            s = ($"{DataType} {Name}: ");
            foreach (var fieldValue in ArrayFields)
            {
                s += fieldValue.PrettyValue();
            }
            return s;
        }
        
        s = ($"{DataType}[] {Name}: [");
        

        int i = 0;
        foreach (var fieldValue in ArrayFields)
        {
           if (!(i == 0))
           {
              s += ", ";
           }
           if (fieldValue.Value.Length == 0)
           {
              s += "noValue";
           }
           else
           {
              s += fieldValue.ToString();
           }
            i = 1;
        }
        s += "]";
        return s;
    }
}
