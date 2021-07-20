using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;
using Xunit;
using Xunit.Abstractions;

namespace Universalis.Application.Tests
{
    public class ArchTests
    {
        private readonly ITestOutputHelper _output;

        public ArchTests(ITestOutputHelper output)
        {
            _output = output;
        }
    }
}
