using System.IO;
using System.Text;
using Xunit;
using RosNet.RosReader;

namespace RosNetTest
{
    public class HeaderTests
    {
        [Fact]
        public void TestReadName()
        {
            // Turn the string into a byte array, then a stream, then make the Binary Reader
            // This is ugly I know
            var reader = new BinaryReader(new MemoryStream(Encoding.ASCII.GetBytes("abcdefg=1234f")));
            Assert.Equal("abcdefg", Header.ReadName(reader));
        }
    }
}
