using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Xunit;

namespace Universalis.DataTransformations.Tests
{
    public class StatisticsTests
    {
        private const long WeekLength = 604800000L;

        [Theory]
        [InlineData(0, new float[] { 3, 3, 3, 3 })]
        [InlineData(1.871, new float[] { 6, 2, 3, 1 })]
        [InlineData(2.983, new float[] { 9, 2, 5, 4, 12, 7, 8, 11, 9, 3, 7, 4, 12, 5, 4, 10, 9, 6, 9, 4 })]
        public void PopulationStd_IsCorrect(float expected, float[] numbers)
        {
            var actual = Statistics.PopulationStd(numbers);
            Assert.Equal(Math.Round(expected, 3), Math.Round(actual, 3));
        }

        [Fact]
        public void GetDistribution_IsCorrect_1()
        {
            var numbers = new[] { 0, 1, 1, 2, 2, 3, 3, 3 };
            var expected = new Dictionary<int, int>
            {
                {0, 1},
                {1, 2},
                {2, 2},
                {3, 3},
            };

            var actual = Statistics.GetDistribution(numbers);
            var actualSorted = SortDictionary(actual);
            foreach (var (e, a) in expected.Zip(actualSorted))
            {
                Assert.Equal(e.Value, a.Value);
            }
        }

        [Fact]
        public void GetDistribution_IsCorrect_2()
        {
            var numbers = new[] { 7, 3, 5, 6, 7, 7, 0, 3 };
            var expected = new Dictionary<int, int>
            {
                {0, 1},
                {3, 2},
                {5, 1},
                {6, 1},
                {7, 3},
            };

            var actual = Statistics.GetDistribution(numbers);
            var actualSorted = SortDictionary(actual);
            foreach (var (e, a) in expected.Zip(actualSorted))
            {
                Assert.Equal(e.Value, a.Value);
            }
        }

        [Theory]
        [Repeat(100)]
        [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "<Pending>")]
        [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "<Pending>")]
        public void WeekVelocityPerDay_IsCorrect1(int i)
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var rand = new Random();
            var timestampsInWeek = GetTimestampsInWeek(now, rand.Next(0, 100)).ToList();
            var timestampsBeforeWeek = GetTimestampsBeforeWeek(rand.Next(0, 100));
            var velocity = Statistics.VelocityPerDay(timestampsInWeek.Concat(timestampsBeforeWeek), now, WeekLength);
            Assert.Equal(timestampsInWeek.Count / 7.0f, velocity);
        }

        [Theory]
        [InlineData(0, new long[] { })]
        public void WeekVelocityPerDay_IsCorrect2(int expected, long[] timestamps)
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var velocity = Statistics.VelocityPerDay(timestamps, now, WeekLength);
            Assert.Equal(expected, velocity);
        }

        private static IEnumerable<long> GetTimestampsInWeek(long now, int count)
        {
            var startOfWeek = now - WeekLength;
            var rand = new Random();

            var timestamps = new List<long>();
            for (var i = 0; i < count - 2; i++)
            {
                timestamps.Add(startOfWeek + (long)Math.Truncate(rand.NextDouble() * WeekLength));
            }

            // Ensure at least one timestamp exists for both the beginning and end of the week.
            timestamps.Add(startOfWeek);
            timestamps.Add(now);

            return timestamps;
        }

        private static IEnumerable<long> GetTimestampsBeforeWeek(int count)
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var startOfWeek = now - WeekLength;
            var rand = new Random();

            var timestamps = new List<long>();
            for (var i = 0; i < count; i++)
            {
                timestamps.Add((long)Math.Truncate(rand.NextDouble() * startOfWeek));
            }

            return timestamps;
        }

        private static IDictionary<int, int> SortDictionary(IDictionary<int, int> dict)
        {
            return dict
                .OrderBy(kvp => kvp.Key)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
