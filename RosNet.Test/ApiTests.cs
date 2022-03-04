using System;
using Xunit;
using System.IO;
using RosNet.DataModel;
using RosNet.RosReader;
using System.Collections.Generic;
using RosNet.Field;
using System.Reflection;

namespace RosNet.Test;

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
        RosBag rosBag = new RosBag(path);
        rosBag.Read();
        Dictionary<string, List<string>> fields = rosBag.GetConnectionFields();

        Assert.Equal(7, fields.Count);
        Assert.True(fields.ContainsKey("/vectornav/IMU"));
        Assert.Equal(8, fields["/sbs/steering_wheel"].Count);

    }

    [Fact]
    public void TestGetTimeSeries()
    {
        string path = GetTestPath("TestBag.bag");
        RosBag rosBag = new RosBag(path);
        rosBag.Read();
        List<(Time, FieldValue)> timeSeries = rosBag.GetTimeSeries("/amk/motor_moment", "FL_motor_moment");

        Assert.Equal((uint) 1622063787, timeSeries.First().Item1.Secs);
        Assert.Equal(0, BitConverter.ToInt32(timeSeries.First().Item2.Value));
    }
}
