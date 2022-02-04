using System.IO;
using System.Text;
using Xunit;
using ROSNET.ROSReader;

namespace ROSNETTest
{
    public class HeaderTests
    {
        [Fact]
        public void TestReadName()
        {
            // Turn the string into a byte array, then a stream, then make the Binary Reader
            // This is ugly I know
            var reader = new BinaryReader(new MemoryStream(Encoding.ASCII.GetBytes("abcdefgh=1234f")));
            Assert.Equal("abcdefg", Header.ReadName(reader));
        }
    }
}
