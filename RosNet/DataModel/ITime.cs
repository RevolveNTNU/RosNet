using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RosNet.DataModel;

internal interface ITime
{
    public uint Secs { get; }
    public uint NSecs { get; }

    /// <summary>
    /// Turns Seconds and Nanoseconds into a DateTime object
    /// </summary>
    /// <returns> A DateTime object corresponding to the Rostime's time </returns>
    public DateTime ToDateTime();
}
