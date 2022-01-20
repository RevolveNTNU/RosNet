using System.IO;
using System.Text.RegularExpressions;
using ROSNET.ROSReader;

namespace ShowCase
{
    class Program
    {

        static void Main(string[] args)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string path = Regex.Replace(currentDirectory, "ROSNET.*", "ROSNET/bags/acc19-11.bag");

            var rosbag = ROSbagReader.Read(path);
            //Console.ReadKey();
        }
    }
}
