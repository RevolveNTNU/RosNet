using System;
using System.IO;

namespace ROSNET.DataModel
{
    public class ROSbag
    {
        private BinaryReader Reader { get; set; }

        public ROSbag()
        {

        }

        public ROSbag(string path)
        {
            Reader = new BinaryReader(File.Open(path, FileMode.Open));
        }
    }
}
