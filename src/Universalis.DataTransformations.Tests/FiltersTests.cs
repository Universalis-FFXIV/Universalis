using System.Collections.Generic;
using Xunit;

namespace Universalis.DataTransformations.Tests;

public class FiltersTests
{
    [Fact]
    public void RemoveOutliers_IsCorrect_1()
    {
        var numbers = new float[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 1000 };
        var expected = new float[] { 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4 };
        TestRemoveOutliers(numbers, expected);
    }

    [Fact]
    public void RemoveOutliers_IsCorrect_2()
    {
        var numbers = new float[] { 1, 2, 3, 4, 5, 6 };
        var expected = new float[] { 1, 2, 3, 4, 5, 6 };
        TestRemoveOutliers(numbers, expected);
    }

    private static void TestRemoveOutliers(IEnumerable<float> numbers, IEnumerable<float> expected)
    {
        var filtered = Filters.RemoveOutliers(numbers, 3);
        Assert.Equal(expected, filtered);
    }
}