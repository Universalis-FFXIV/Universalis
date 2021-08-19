using System.Text.Json;
using Xunit;

namespace Universalis.Application.Tests
{
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
        public void ParseUnusualBool_Works(string input, bool expected)
        {
            var de = JsonSerializer.Deserialize<ParseUnusualBoolTestClass>(input);
            Assert.NotNull(de);
            var actual = Util.ParseUnusualBool(de.Hq);
            Assert.Equal(expected, actual);
        }

        private class ParseUnusualBoolTestClass
        {
            public object Hq { get; set; }
        }
    }
}