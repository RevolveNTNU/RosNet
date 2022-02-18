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

            Assert.Equal(fields.Count, 39);
            Assert.True(fields.ContainsKey("/amk/rpm"));
            Assert.Equal(fields["/sbs/steering_wheel"].Count, 6);

        }

        [Fact]
        public void TestGetTimeSeries()
        {
            string path = Directory.GetCurrentDirectory() + "/TestBags/acc19-11.bag";
            RosBag rosBag = RosBagReader.Read(path);
            Dictionary<(uint, uint), FieldValue> timeSeries = rosBag.GetTimeSeries("/amk/rpm", "FL_vel");

            Assert.True(BitConverter.ToInt32(timeSeries[(1629366496, 951464957)].Value), 7071);

        }
    }
}
