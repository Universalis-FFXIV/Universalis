using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit.Sdk;

namespace Universalis.DataTransformations.Tests;

public class RepeatAttribute : DataAttribute
{
    // https://stackoverflow.com/a/31878640/14226597
    private readonly int _count;

    public RepeatAttribute(int count)
    {
        if (count < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(count),
                "Repeat count must be greater than 0.");
        }
        _count = count;
    }

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var data = new List<object[]>();
        for (var i = 0; i < _count; i++)
        {
            data.Add(new object[] { i });
        }

        return data;
    }
}