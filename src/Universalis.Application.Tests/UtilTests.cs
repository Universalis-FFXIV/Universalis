using System.Text.Json;
using Xunit;

namespace Universalis.Application.Tests;

public class UtilTests
{
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