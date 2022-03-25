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
        Assert.Equal(test.ToDateTime(), new DateTime(2022, 02, 15, 14, 35, 8).AddSeconds(epochNano * Math.Pow(10, -9)));
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
    public void TestHashCode()
    {
        string path1 = ApiTests.GetTestPath("HashCodeTest1.bag");
        string path2 = ApiTests.GetTestPath("HashCodeTest2.bag");
        RosBag bag1 = new RosBag(path1);
        bag1.Read();
        RosBag bag1copy = new RosBag(path1);
        bag1copy.Read();
        RosBag bag2 = new RosBag(path2);
        bag2.Read();
        Assert.Equal(bag1.GetHashCode(), bag1copy.GetHashCode());
        Assert.NotEqual(bag1.GetHashCode(), bag2.GetHashCode());
    }
}
