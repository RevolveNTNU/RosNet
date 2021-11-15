using System;
using System.Collections.Generic;
using System.Text;

namespace ROSNET.DataModel
{
    public class Message
    {
        public int ConnID { get; }
        public long Time { get;  }
        private byte[] Data { get; }

        public Message(int connID, long time, byte[] data)
        {
            ConnID = connID;
            Time = time;
            Data = data;
        }
    }
}
