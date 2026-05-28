using BBDown.Core.Util;

namespace BBDown.Tests;

public class BilibiliBvConverterTests
{
    [Theory]
    [InlineData(170001, "BV1bK411x7ct")]
    [InlineData(455017605, "BV1HK411a7na")]
    [InlineData(882584971, "BV1mK4y1C7Bz")]
    public void Encode_ReturnsExpectedBv(long aid, string expectedBv)
    {
        var result = BilibiliBvConverter.Encode(aid);
        Assert.Equal(expectedBv, result);
    }

    [Theory]
    [InlineData("bK411x7ct", 170001)]
    [InlineData("HK411a7na", 455017605)]
    [InlineData("mK4y1C7Bz", 882584971)]
    public void Decode_ReturnsExpectedAid(string bvSuffix, long expectedAid)
    {
        var result = BilibiliBvConverter.Decode(bvSuffix);
        Assert.Equal(expectedAid, result);
    }

    [Fact]
    public void Encode_TooSmallAid_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BilibiliBvConverter.Encode(0));
    }

    [Fact]
    public void Decode_WrongLength_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => BilibiliBvConverter.Decode("short"));
    }
}
