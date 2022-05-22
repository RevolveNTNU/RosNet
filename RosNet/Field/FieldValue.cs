using RosNet.Type;

namespace RosNet.Field;

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
        this.Value = null;
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
        if (this.Value == null)
        {
            return ($"{DataType} {Name} noValue");
        }
        return ($"{DataType} {Name} {PrettyValue()}");
    }

    /// <summary>
    /// Creates string of Value using datatype
    /// </summary>
    /// <returns>String of Value</returns>
    public string PrettyValue() => this.DataType switch
    {
        PrimitiveType.Bool => ($"{BitConverter.ToBoolean(this.Value)}"),
        PrimitiveType.Int8 => ((int)this.Value.First()).ToString(),
        PrimitiveType.Uint8 => ((uint)this.Value.First()).ToString(),
        PrimitiveType.Byte => ($"{(sbyte)this.Value.First()}"),
        PrimitiveType.Char => ((char)this.Value.First()).ToString(),
        PrimitiveType.Int16 => ($"{BitConverter.ToInt16(this.Value)}"),
        PrimitiveType.Uint16 => ($"{BitConverter.ToUInt16(this.Value)}"),
        PrimitiveType.Int32 => ($"{BitConverter.ToInt32(this.Value)}"),
        PrimitiveType.Uint32 => ($"{BitConverter.ToUInt32(this.Value)}"),
        PrimitiveType.Int64 => ($"{BitConverter.ToInt64(this.Value)}"),
        PrimitiveType.Uint64 => ($"{BitConverter.ToUInt64(this.Value)}"),
        PrimitiveType.Float32 => ($"{BitConverter.ToSingle(this.Value)}"),
        PrimitiveType.Float64 => ($"{BitConverter.ToDouble(this.Value)}"),
        PrimitiveType.String => System.Text.Encoding.Default.GetString(this.Value),
        PrimitiveType.Time => ($"{BitConverter.ToUInt32(this.Value.Take(4).ToArray())} : {BitConverter.ToUInt32(this.Value.Skip(4).Take(4).ToArray())}"),
        PrimitiveType.Duration => ($"{BitConverter.ToInt32(this.Value.Take(4).ToArray())} : {BitConverter.ToInt32(this.Value.Skip(4).Take(4).ToArray())}"),
        _ => throw new RosBagException($"The datatype {this.DataType.ToString()} is not a primitive type")

    };
}
