using System;
using System.Collections.Generic;
using System.Text;

namespace RosNet.DataModel;

public class Time : IComparable<Time>, ITime
{
    // Time data stored as they come in
    public uint Secs { get; }
    public uint NSecs { get; }

    /// <summary>
    /// Create a RosTime object from seconds and nanoseconds
    /// </summary>
    public Time(uint secs, uint nsecs)
    {
        this.Secs = secs;
        this.NSecs = nsecs;
    }

    /// <summary>
    /// Turns Seconds and Nanoseconds into a DateTime object
    /// </summary>
    /// <returns> A DateTime object corresponding to the Rostime's time </returns>
    public DateTime ToDateTime()
    {
        // Ros timestamps are usually saved as epoch time
        double secs = this.Secs + this.NSecs * Math.Pow(10, -9);
        return DateTime.UnixEpoch.AddSeconds(secs);
    }

    /// <summary>
    /// Compares two time objects by comparing sec and nano sec
    /// </summary>
    public int CompareTo(Time? other)
    {
        if (other == null)
            return 1;

        return this.Secs != other.Secs ? this.Secs.CompareTo(other.Secs) : this.NSecs.CompareTo(other.NSecs);
    }
}
