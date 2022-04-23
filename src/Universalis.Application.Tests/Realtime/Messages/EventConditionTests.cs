using Universalis.Application.Realtime.Messages;
using Universalis.Application.Tests.Mocks.Realtime.Messages;
using Xunit;

namespace Universalis.Application.Tests.Realtime.Messages;

public class EventConditionTests
{
    [Theory]
    [InlineData("a", true)]
    [InlineData("b", false)]
    public void Condition_Parsed_OneChannel(string input, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var message = new MockMessage("a", "b", "c") { Value = 8 };
        Assert.Equal(expected, condition.ShouldSend(message));
    }

    [Theory]
    [InlineData("a/b", true)]
    [InlineData("b/a", false)]
    public void Condition_Parsed_TwoChannels(string input, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var message = new MockMessage("a", "b", "c") { Value = 8 };
        Assert.Equal(expected, condition.ShouldSend(message));
    }

    [Theory]
    [InlineData("a/b/c", true)]
    [InlineData("a/c/b", false)]
    [InlineData("b/a/c", false)]
    [InlineData("b/c/a", false)]
    [InlineData("c/b/a", false)]
    [InlineData("c/a/b", false)]
    public void Condition_Parsed_ThreeChannels(string input, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var message = new MockMessage("a", "b", "c") { Value = 8 };
        Assert.Equal(expected, condition.ShouldSend(message));
    }

    [Theory]
    [InlineData("a/b/c/d", false)]
    public void Condition_Parsed_FourChannels(string input, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var message = new MockMessage("a", "b", "c") { Value = 8 };
        Assert.Equal(expected, condition.ShouldSend(message));
    }

    [Theory]
    [InlineData("a{value = 8}", true)]
    [InlineData("a{Value = 8}", false)]
    [InlineData("a{value=8}", true)]
    [InlineData("a{Value=8}", false)]
    [InlineData("a{value=7}", false)]
    [InlineData("b{}", false)]
    [InlineData("b{value=8}", false)]
    public void Condition_Parsed_OneChannel_Filter(string input, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var message = new MockMessage("a", "b", "c") { Value = 8 };
        Assert.Equal(expected, condition.ShouldSend(message));
    }

    [Theory]
    [InlineData("a/b{value = 8}", true)]
    [InlineData("a/b{Value = 8}", false)]
    [InlineData("a/b{value=8}", true)]
    [InlineData("a/b{Value=8}", false)]
    [InlineData("a/b{value=7}", false)]
    [InlineData("b/a{}", false)]
    [InlineData("b/a{value=8}", false)]
    public void Condition_Parsed_TwoChannels_Filter(string input, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var message = new MockMessage("a", "b", "c") { Value = 8 };
        Assert.Equal(expected, condition.ShouldSend(message));
    }

    [Theory]
    [InlineData("a/b/c{value = 8}", true)]
    [InlineData("a/b/c{Value = 8}", false)]
    [InlineData("a/b/c{value=8}", true)]
    [InlineData("a/b/c{Value=8}", false)]
    [InlineData("a/b/c{value=7}", false)]
    [InlineData("b/a/c{}", false)]
    [InlineData("b/a/c{value=8}", false)]
    public void Condition_Parsed_ThreeChannels_Filter(string input, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var message = new MockMessage("a", "b", "c") { Value = 8 };
        Assert.Equal(expected, condition.ShouldSend(message));
    }

    [Theory]
    [InlineData("a/b/c/d{value = 8}", false)]
    [InlineData("a/b/c/d{Value = 8}", false)]
    [InlineData("a/b/c/d{value=8}", false)]
    [InlineData("a/b/c/d{Value=8}", false)]
    [InlineData("a/b/c/d{value=7}", false)]
    [InlineData("b/a/c/d{}", false)]
    [InlineData("b/a/c/d{value=8}", false)]
    public void Condition_Parsed_FourChannels_Filter(string input, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var message = new MockMessage("a", "b", "c") { Value = 8 };
        Assert.Equal(expected, condition.ShouldSend(message));
    }
}