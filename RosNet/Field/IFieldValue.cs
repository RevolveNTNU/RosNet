using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RosNet.Type;

namespace RosNet.Field;

internal interface IFieldValue
{
    public string Name { get; set; }
    public PrimitiveType DataType { get; }
    public byte[] Value { get; }

    /// <summary>
    /// Finds the byte length of the fieldvalue using the datatype
    /// </summary>
    /// <returns>byte length of fieldvalue</returns>
    public int GetByteLength();

    /// <summary>
    /// Creates string with value
    /// </summary>
    /// <returns>String with value</returns>
    public string ToString();

    /// <summary>
    /// Creates string of Value using datatype
    /// </summary>
    /// <returns>String of Value</returns>
    public string PrettyValue();
}
