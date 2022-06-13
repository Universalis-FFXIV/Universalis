using System;
using Universalis.Application.Realtime.Messages;
using Universalis.Application.Tests.Mocks.Realtime.Messages;
using Xunit;

namespace Universalis.Application.Tests.Realtime.Messages;

public class EventConditionTests
{
    [Fact]
    public void Condition_Throws_NoChannels()
    {
        Assert.ThrowsAny<Exception>(() => EventCondition.Parse(""));
    }
    
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
    
    [Theory]
    [InlineData("a", "a", true)]
    [InlineData("a", "a{}", true)]
    [InlineData("a", "a{value = 8}", false)]
    [InlineData("a", "a/b", false)]
    [InlineData("a", "a/b{}", false)]
    [InlineData("a", "a/b{value = 8}", false)]
    [InlineData("b", "a", false)]
    [InlineData("b", "a{}", false)]
    [InlineData("b", "a{value = 8}", false)]
    [InlineData("b", "a/b", false)]
    [InlineData("b", "a/b{}", false)]
    [InlineData("b", "a/b{value = 8}", false)]
    public void Condition_IsReplaceableWith_OneChannel(string input, string otherInput, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var conditionOther = EventCondition.Parse(otherInput);
        Assert.Equal(expected, condition.IsReplaceableWith(conditionOther));
    }
    
    [Theory]
    [InlineData("a/b", "a", true)]
    [InlineData("a/b", "a/b", true)]
    [InlineData("a/b", "a/b/c", false)]
    [InlineData("a/b", "b", false)]
    [InlineData("a/b", "b/a", false)]
    [InlineData("a/b", "b/a/c", false)]
    [InlineData("b/a", "b", true)]
    [InlineData("b/a", "b/a", true)]
    [InlineData("b/a", "b/a/c", false)]
    [InlineData("b/a", "a", false)]
    [InlineData("b/a", "a/b", false)]
    [InlineData("b/a", "a/b/c", false)]
    public void Condition_IsReplaceableWith_TwoChannels(string input, string otherInput, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var conditionOther = EventCondition.Parse(otherInput);
        Assert.Equal(expected, condition.IsReplaceableWith(conditionOther));
    }
    
    [Theory]
    [InlineData("a{value = 8}", "a", true)]
    [InlineData("a{value = 8}", "a{}", true)]
    [InlineData("a{value = 8}", "a{value = 8}", true)]
    [InlineData("a{value = 8}", "a{value = 7}", false)]
    [InlineData("a{value = 8}", "a/b", false)]
    [InlineData("a{value = 8}", "a/b{}", false)]
    [InlineData("a{value = 8}", "a/b{value = 8}", false)]
    [InlineData("b{value = 8}", "a", false)]
    [InlineData("b{value = 8}", "a{}", false)]
    [InlineData("b{value = 8}", "a{value = 8}", false)]
    [InlineData("b{value = 8}", "a/b", false)]
    [InlineData("b{value = 8}", "a/b{}", false)]
    [InlineData("b{value = 8}", "a/b{value = 8}", false)]
    public void Condition_IsReplaceableWith_OneChannel_Filter(string input, string otherInput, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var conditionOther = EventCondition.Parse(otherInput);
        Assert.Equal(expected, condition.IsReplaceableWith(conditionOther));
    }
    
    [Theory]
    [InlineData("a/b{value = 8}", "a", true)]
    [InlineData("a/b{value = 8}", "a/b", true)]
    [InlineData("a/b{value = 8}", "a/b/c", false)]
    [InlineData("a/b{value = 8}", "b", false)]
    [InlineData("a/b{value = 8}", "b/a", false)]
    [InlineData("a/b{value = 8}", "b/a/c", false)]
    [InlineData("b/a{value = 8}", "a", false)]
    [InlineData("b/a{value = 8}", "a/b", false)]
    [InlineData("b/a{value = 8}", "a/b/c", false)]
    [InlineData("b/a{value = 8}", "b/a/c", false)]
    public void Condition_IsReplaceableWith_TwoChannels_Filter(string input, string otherInput, bool expected)
    {
        var condition = EventCondition.Parse(input);
        var conditionOther = EventCondition.Parse(otherInput);
        Assert.Equal(expected, condition.IsReplaceableWith(conditionOther));
    }
}