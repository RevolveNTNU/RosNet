using System;
using RosNet.DataModel;
using Xunit;

namespace RosNet.Test;

public class DataModelTests
{
    [Fact]
    public void TestToDateTime()
    {
        // Epoch time corresponding to
        // 2022-02-15 14:35:08.1234567
        uint epochSec = 1644935708;
        uint epochNano = 123456700;
        Time test = new Time(epochSec, epochNano);
        // 1 tick is 100 nano seconds
        Assert.Equal(new DateTime(2022, 02, 15, 14, 35, 8).AddSeconds(epochNano * Math.Pow(10, -9)), test.ToDateTime());
    }

    [Fact]
    public void TestBagStartTime()
    {
        string path = ApiTests.GetTestPath("DataModelTestBag.bag");
        RosBag rosBag = new RosBag(path);
        rosBag.Read();
        Assert.Equal(new Time(1622060004, 130476520), rosBag.BagStartTime);
    }

    [Fact]
    public void TestCompareToTime()
    {
        uint firstEpochSec = 1644935708;
        uint firstEpochNano = 123456700;
        Time testOne = new Time(firstEpochSec, firstEpochNano);
        uint secondEpochSec = 1651667525;
        uint secondEpochNano = 123456700;
        Time testTwo = new Time(secondEpochSec, secondEpochNano);
        uint thirdEpochSec = 1651667525;
        uint thirdEpochNano = 123456699;
        Time testThree = new Time(thirdEpochSec, thirdEpochNano);
        Assert.Equal(testOne.CompareTo(testOne), 0);
        Assert.True(testThree.CompareTo(testTwo) < 0);
        Assert.True(testTwo.CompareTo(testThree) > 0);
        Assert.Equal(testOne.CompareTo(null), 1);
    }

    [Fact]
    public void TestToString()
    {
        string path = ApiTests.GetTestPath("DataModelTestBag.bag");
        RosBag rosBag = new RosBag(path);
        rosBag.Read();
        Assert.False(rosBag.ToString().Equals(null));
    }
}
