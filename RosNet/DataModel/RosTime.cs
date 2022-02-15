using System;
using System.Collections.Generic;
using System.Text;

namespace RosNet.DataModel
{
    public class RosTime
    {
        // Time data stored as they come in
        public uint Secs { get; }
        public uint NSecs { get; }

        /// <summary>
        /// Create a RosTime object from seconds and nanoseconds
        /// </summary>
        public RosTime(uint secs, uint nsecs)
        {
            this.Secs = secs;
            this.NSecs = nsecs;
        }
    }
}
