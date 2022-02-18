using System;
using Xunit;
using System.IO;
using RosNet.DataModel;
using RosNet.RosReader;
using System.Collections.Generic;
using RosNet.Field;

namespace RosNet.Test
{
    public class ApiTests
    {
        [Fact]
        public void TestGetConnectionFields()
        {
            string path = Directory.GetCurrentDirectory() + "/TestBags/acc19-11.bag";
            RosBag rosBag = RosBagReader.Read(path);
            Dictionary<string, List<string>> fields = rosBag.GetConnectionFields();

            Assert.Equal(39, fields.Count);
            Assert.True(fields.ContainsKey("/amk/rpm"));
            Assert.Equal(6, fields["/sbs/steering_wheel"].Count);

        }

        [Fact]
        public void TestGetTimeSeries()
        {
            string path = Directory.GetCurrentDirectory() + "/TestBags/acc19-11.bag";
            RosBag rosBag = RosBagReader.Read(path);
            Dictionary<(uint, uint), FieldValue> timeSeries = rosBag.GetTimeSeries("/amk/rpm", "FL_vel");

            Assert.Equal(7071, BitConverter.ToInt32(timeSeries[(1629366496, 951464957)].Value));
        }
    }
}
