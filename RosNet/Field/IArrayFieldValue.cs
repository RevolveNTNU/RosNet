using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosNet.Field;

internal interface IArrayFieldValue
{
    public List<FieldValue> ArrayFields { get; set; }
    public uint FixedArrayLength { get; } //used if array has fixed length

    /// <summary>
    /// Finds the byte length of the array using fields in array
    /// </summary>
    /// <returns>byte length of array</returns>
    public int GetByteLength();

    /// <summary>
    /// Creates string with values
    /// </summary>
    /// <returns>string with values</returns>
    public string ToString();
}
