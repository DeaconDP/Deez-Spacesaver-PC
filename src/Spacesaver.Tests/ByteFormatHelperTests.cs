using Spacesaver.Helpers;

namespace Spacesaver.Tests;

public class ByteFormatHelperTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(500, "500 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(512_000, "500 KB")]
    [InlineData(5L * 1024 * 1024, "5 MB")]
    [InlineData(1024L * 1024 * 1024, "1 GB")]
    [InlineData(10L * 1024 * 1024 * 1024, "10 GB")]
    public void FormatBytes_UsesCorrectUnit(long bytes, string expected)
    {
        Assert.Equal(expected, ByteFormatHelper.FormatBytes(bytes));
    }

    [Fact]
    public void FormatBytes_FractionalTerabytes_UsesCurrentCultureDecimals()
    {
        // Decimal separator follows the user's culture ("1.5" vs "1,5").
        var expected = $"{1.5:0.##} TB";
        Assert.Equal(expected, ByteFormatHelper.FormatBytes(1536L * 1024 * 1024 * 1024));
    }

    [Fact]
    public void FormatBytes_FractionalMegabytes_KeepsTwoDecimals()
    {
        var bytes = (long)(2.25 * 1024 * 1024);
        Assert.Equal($"{2.25:0.##} MB", ByteFormatHelper.FormatBytes(bytes));
    }
}
