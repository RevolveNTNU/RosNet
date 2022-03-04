using System.Collections.ObjectModel;
using RosNet.Field;

namespace RosNet.DataModel;

/// <summary>
/// Represents a ROSbag message
/// </summary>
public class Message
{
    //Header fields of message record:
    public int Conn { get; }
    public Time TimeStamp { get;  }

    //Data in message record:
    public ReadOnlyDictionary<string,FieldValue> Data { get; internal set; }

    /// <summary>
    /// Create a message with conn and time from message record header
    /// </summary>
    public Message(FieldValue conn, FieldValue time)
    {
        this.Conn = BitConverter.ToInt32(conn.Value);
        uint secs = BitConverter.ToUInt32(time.Value.Take(4).ToArray());
        uint nsecs = BitConverter.ToUInt32(time.Value.Skip(4).Take(4).ToArray());
        this.TimeStamp = new Time(secs, nsecs);
    }

    public override string ToString()
    {
        var s = ($"Conn: {Conn} \n");
        s += ($"Time: {TimeStamp.ToDateTime().ToString("o")} \n");
        s += "Data: \n";
        foreach (KeyValuePair<string, FieldValue> kvp in Data)
        {
            s += ($"{kvp.Value.ToString()} \n");
        }

        return s;
    }
}
