using RosNet.Field;

namespace RosNet.DataModel;

/// <summary>
/// Represents a ROSbag
/// </summary>
public class RosBag
{
    public Dictionary<int, Connection> Connections { get; private set; } //List of connection records in rosbag

    /// <summary>
    /// Creates an empty rosbag
    /// </summary>
    public RosBag()
    {
        Connections = new Dictionary<int, Connection>();
    }

    /// <summary>
    /// Adds a Connection object to the ROSbag's list of connections
    /// </summary>
    /// <returns>true if connection was successfully added</returns>
    internal bool AddConnection(Connection conn)
    {
        if (!Connections.ContainsKey(conn.Conn))
        {
            Connections.Add(conn.Conn, conn);
            return true;
        }
        return false;
    }
    
    public override string ToString()
    {
        string s = "ROSbag \n";
        s += "Connections:";
        foreach (KeyValuePair<int, Connection> kvp in Connections)
        {
            s += ($"Connection: {kvp.Key} \n");
            s += kvp.Value.ToString();
        }
        return s;
    }

    /// <summary>
    /// Makes a dictionary of all the topics and fieldnames in rosbag
    /// </summary>
    /// <returns>Dictionary with topics and corresponding fieldnames</returns>
    public Dictionary<string, List<string>> GetConnectionFields()
    {
        var fieldsByTopic = new Dictionary<string, List<string>>();
        foreach (Connection conn in Connections.Values)
        {
            fieldsByTopic.Add(conn.Topic, new List<string>());
            foreach (FieldValue field in conn.MessageDefinition)
            {
                fieldsByTopic[conn.Topic].Add(field.Name);
            }
        }
        return fieldsByTopic;
    }

    /// <summary>
    /// Makes a dictionary of timestamps and fieldvalues of field with fieldname fieldName
    /// </summary>
    /// <returns>Dictionary with time and corresponding fieldvalue</returns>
    public List<(Time, FieldValue)> GetTimeSeries(string topic, string fieldName)
    {
        var timeSeries = new List<(Time, FieldValue)>();

        //todo:finn bedre løsning
        foreach (Connection conn in Connections.Values)
        {
            if (!conn.Topic.Equals(topic))
            {
                continue;
            }

            if (!conn.MessageDefinition.Exists(field => field.Name.Equals(fieldName)))
            {
                continue;
            }

            foreach (Message message in conn.Messages)
            {
                timeSeries.Add((message.TimeStamp, message.Data[fieldName]));
            }
        }

        //todo: raise exception if timeseries is empty
        return timeSeries;
    }
}
