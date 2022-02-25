using System.Collections.ObjectModel;

using RosNet.Field;

namespace RosNet.DataModel;

internal interface IMessage
{
    //Header fields of message record:
    public int Conn { get; }
    public Time TimeStamp { get; }

    //Data in message record:
    public ReadOnlyDictionary<string, FieldValue> Data { get; }

    public string ToString();
}
