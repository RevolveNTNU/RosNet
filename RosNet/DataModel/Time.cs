using System;
using System.Collections.Generic;
using System.Text;

namespace RosNet.DataModel
{
    public class Time : IComparable, ITime
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
        public int CompareTo(object? other)
        {
            if (other == null)
                return 1;

            Time otherTime = other as Time;

            if (otherTime == null)
                throw new ArgumentException("Other is not of type Time");

            if (this.Secs - otherTime.Secs != 0)
            {
                return (int)(this.Secs - otherTime.Secs);
            }
            else if (this.NSecs - otherTime.NSecs != 0)
            {
                return (int)(this.NSecs - otherTime.NSecs);
            }
            return 0;
        }
    }
}
