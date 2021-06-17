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

        [Fact]
        public void DbAccess_Should_Only_Have_Contexts()
        {
            var result = Types.InAssembly(typeof(Startup).Assembly)
                .That().ResideInNamespaceContaining("DbAccess")
                .Should().Inherit(typeof(DbContext))
                .And().HaveNameEndingWith("Context")
                .GetResult();

            if (!result.IsSuccessful)
            {
                _output.WriteLine(string.Join(',', result.FailingTypeNames));
            }
            
            result.IsSuccessful.Should().BeTrue();
        }
    }
}
