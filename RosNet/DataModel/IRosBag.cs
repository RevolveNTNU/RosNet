using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RosNet.Field;

namespace RosNet.DataModel;

internal interface IRosBag
{
    public Dictionary<int, Connection> Connections { get; }

    public string ToString();

    /// <summary>
    /// Makes a dictionary of all the topics and fieldnames in rosbag
    /// </summary>
    /// <returns>Dictionary with topics and corresponding fieldnames</returns>
    public Dictionary<string, List<string>> GetConnectionFields();

    /// <summary>
    /// Makes a dictionary of timestamps and fieldvalues of field with fieldname fieldName
    /// </summary>
    /// <returns>Dictionary with time and corresponding fieldvalue</returns>
    public List<(Time, FieldValue)> GetTimeSeries(string topic, string fieldName);
}
