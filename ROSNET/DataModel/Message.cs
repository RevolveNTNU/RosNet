using System;
using System.Collections.Generic;
using System.Text;

namespace ROSNET.DataModel
{
    public class Message
    {
        public int Conn { get; }
        public long Time { get;  }
        private byte[] Data { get; }

        public Message(int conn, long time, byte[] data)
        {
            Conn = conn;
            Time = time;
            Data = data;
        }
    }
}
