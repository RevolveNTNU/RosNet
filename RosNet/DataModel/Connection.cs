using System;
using System.Collections.Generic;
using RosNet.Field;

namespace RosNet.DataModel;

/// <summary>
/// Represents a ROSbag connection
/// </summary>
public class Connection : IConnection
{
    //Header fields in Connection record:
    public int Conn { get; } //unique ID for each connection
    public string Topic { get; }

    //Data fields in Connection record:
    public string OriginalTopic { get; private set; }
    public string Type { get; private set; }
    public string Md5sum { get; private set; }
    public List<FieldValue> MessageDefinition { get; private set; } //defines how to read the message data of messages corresponding with this connection
    public string CallerID { get; private set; }
    public string Latching { get; private set; }

    //List of messages corresponding to this connection
    public List<Message> Messages { get; private set; }

    /// <summary>
    /// Create a connection with conn and topic from connection record header
    /// </summary>
    public Connection(FieldValue conn, FieldValue topic)
    {
        Conn = BitConverter.ToInt32(conn.Value);
        Topic = System.Text.Encoding.Default.GetString(topic.Value);
        Messages = new List<Message>();
    }

    /// <summary>
    /// Sets data in connection
    /// </summary>
    internal void SetData(byte[] originalTopic, byte[] type, byte[] md5sum, List<FieldValue> messageDefinition, byte[] callerID, byte[] latching)
    {
        this.OriginalTopic = System.Text.Encoding.Default.GetString(originalTopic);
        this.Type = System.Text.Encoding.Default.GetString(type);
        this.Md5sum = System.Text.Encoding.Default.GetString(md5sum);
        this.MessageDefinition = messageDefinition;
        this.CallerID = System.Text.Encoding.Default.GetString(callerID);
        this.Latching = System.Text.Encoding.Default.GetString(latching);
    }

    public override string ToString()
    {
        string s = ($"Conn: {Conn} \n");
        s += ($"Topic: {Topic} \n");
        s += ($"OriginalTopic: {OriginalTopic} \n");
        s += ($"Type: {Type} \n");
        s += ($"Md5sum: {Md5sum} \n");
        s += "MessageDefinition: \n";
        foreach (var dataField in MessageDefinition)
        {
            s += ($"{dataField.ToString()} \n");
        }
        s += ($"CallerID: {CallerID} \n");
        s += ($"Latching: {Latching} \n");
        s += "Messages connected to this connection: \n";

        foreach (Message message in Messages)
        {
            s += ($"{message.ToString()} \n");
        }
        return s;
    }
}
