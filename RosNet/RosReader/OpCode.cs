using System;
namespace RosNet.RosReader
{
    public enum OpCode
    {
        MessageData = 0x02, 
        BagHeader   = 0x03,
        IndexData   = 0x04,
        Chunk       = 0x05,
        ChunkInfo   = 0x06,
        Connection  = 0x07
    }
}

