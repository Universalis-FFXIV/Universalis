using System.Text.Json;
using Xunit;

namespace Universalis.Application.Tests;

public class UtilTests
{
    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(1, 13, 0)]
    [InlineData(9, 23, 10)]
    [InlineData(9999, 1, 499)]
    [InlineData(10000, 1, 500)]
    [InlineData(189972, 1, 9498)]
    [InlineData(200, 55, 550)]
    public void CalculateTax_Works(int unitPrice, int quantity, int expected)
    {
        var actual = Util.CalculateTax(unitPrice, quantity);
        Assert.Equal(expected, actual);
    }
    
    [Theory]
    [InlineData("{\"Hq\":true}", true)]
    [InlineData("{\"Hq\":false}", false)]
    [InlineData("{\"Hq\":\"true\"}", true)]
    [InlineData("{\"Hq\":\"false\"}", false)]
    [InlineData("{\"Hq\":\"True\"}", true)]
    [InlineData("{\"Hq\":\"False\"}", false)]
    [InlineData("{\"Hq\":1}", true)]
    [InlineData("{\"Hq\":0}", false)]
    [InlineData("{\"Hq\":\"1\"}", true)]
    [InlineData("{\"Hq\":\"0\"}", false)]
    [InlineData("{\"Hq\":null}", false)]
    public void ParseUnusualBool_Works_Json(string input, bool expected)
    {
        var de = JsonSerializer.Deserialize<ParseUnusualBoolTestClass>(input);
        Assert.NotNull(de);
        var actual = Util.ParseUnusualBool(de.Hq);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ParseUnusualBool_Works_Native(object input, bool expected)
    {
        var actual = Util.ParseUnusualBool(input);
        Assert.Equal(expected, actual);
    }

    private class ParseUnusualBoolTestClass
    {
        public object Hq { get; set; }
    }
}