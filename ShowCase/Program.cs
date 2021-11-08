using System;
using System.IO;
using System.Text.RegularExpressions;
using ROSNET;

namespace ShowCase
{
    class Program
    {

        static void Main(string[] args)
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string path = Regex.Replace(currentDirectory, "ROSNET.*", "ROSNET/bags/acc19-11.bag");

            Reader.Read(path);
            Console.ReadKey();
        }
    }
}
