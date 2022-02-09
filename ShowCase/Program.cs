using System.IO;
using System.Text.RegularExpressions;
using RosNet.RosReader;

namespace ShowCase
{
    class Program
    {
        static void Main(string[] args)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string path = Regex.Replace(currentDirectory, "RosNet.*", "RosNet/bags/acc19-11.bag");

            var rosBag = RosBagReader.Read(path);
            //Console.ReadKey();
        }
    }
}
