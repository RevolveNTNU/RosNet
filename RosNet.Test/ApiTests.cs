using System;
using Xunit;
using System.IO;
using RosNet.DataModel;
using RosNet.RosReader;
using System.Collections.Generic;
using RosNet.Field;
using System.Reflection;

namespace RosNet.Test
{
    public class ApiTests
    {

        public static string GetTestPath(string relativePath)
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().Location);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            return Path.Combine(dirPath, "TestBags", relativePath);
        }


        [Fact]
        public void TestGetConnectionFields()
        {
            string path = GetTestPath("TestBag.bag");
            RosBag rosBag = RosBagReader.Read(path);
            Dictionary<string, List<string>> fields = rosBag.GetConnectionFields();

            Assert.Equal(7, fields.Count);
            Assert.True(fields.ContainsKey("/vectornav/IMU"));
            Assert.Equal(9, fields["/sbs/steering_wheel"].Count);

        }

        [Fact]
        public void TestGetTimeSeries()
        {
            string path = GetTestPath("TestBag.bag");
            RosBag rosBag = RosBagReader.Read(path);
            Dictionary<Time, FieldValue> timeSeries = rosBag.GetTimeSeries("/amk/rpm", "FL_vel");

            Assert.Equal(-0.001396, BitConverter.ToInt32(timeSeries[new Time(1622063787, 689419290)].Value));
        }
    }
}
