using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RosNet.Field;

namespace RosNet.DataModel;

internal interface IConnection
{
    //Header fields in Connection record:
    public int Conn { get; } //unique ID for each connection
    public string Topic { get; }

    //Data fields in Connection record:
    public string OriginalTopic { get; }
    public string Type { get; }
    public string Md5sum { get; }
    public List<FieldValue> MessageDefinition { get; } //defines how to read the message data of messages corresponding with this connection
    public string CallerID { get; }
    public string Latching { get; }

    //List of messages corresponding to this connection
    public List<Message> Messages { get; }

    public string ToString();
}
