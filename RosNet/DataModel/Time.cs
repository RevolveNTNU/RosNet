using System;
using System.Collections.Generic;
using System.Text;

namespace RosNet.DataModel
{
    public class Time
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
        public DateTime GetDateTime()
        {
            // A DateTime tick is defined as 100 nanoseconds
            long ticks = 0;
            ticks += this.Secs * 10_000_000;
            ticks += this.NSecs / 100;
            return new DateTime(ticks);
        }
    }
}
