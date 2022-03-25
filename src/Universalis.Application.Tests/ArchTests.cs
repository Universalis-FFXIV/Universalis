using NetArchTest.Rules;
using System;
using Universalis.Application.Uploads.Behaviors;
using Universalis.Application.Views;
using Xunit;
using Xunit.Abstractions;

namespace Universalis.Application.Tests;

public class ArchTests
{
    private readonly ITestOutputHelper _output;

    public ArchTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Views_AreSiloed()
    {
        var result = Types.InAssembly(typeof(ListingView).Assembly)
            .That()
            .ResideInNamespace("Universalis.Application.Views")
            .Should()
            .NotHaveDependencyOnAny("Universalis.DbAccess", "Universalis.Entities")
            .GetResult();
        _output.WriteLine(string.Join(',', result.FailingTypeNames ?? Array.Empty<string>()));
        Assert.True(result.IsSuccessful, "API views should not rely on database models.");
    }

    [Fact]
    public void UploadSchema_AreSiloed()
    {
        var result = Types.InAssembly(typeof(ListingView).Assembly)
            .That()
            .ResideInNamespace("Universalis.Application.Uploads.Schema")
            .Should()
            .NotHaveDependencyOnAny("Universalis.DbAccess", "Universalis.Entities")
            .GetResult();
        _output.WriteLine(string.Join(',', result.FailingTypeNames ?? Array.Empty<string>()));
        Assert.True(result.IsSuccessful, "API views should not rely on database models.");
    }

    [Fact]
    public void UploadBehaviors_ImplementInterface()
    {
        var result = Types.InAssembly(typeof(IUploadBehavior).Assembly)
            .That()
            .ResideInNamespace("Universalis.Application.Uploads.Behaviors")
            .And()
            .AreClasses()
            .Should()
            .ImplementInterface(typeof(IUploadBehavior))
            .GetResult();
        _output.WriteLine(string.Join(',', result.FailingTypeNames ?? Array.Empty<string>()));
        Assert.True(result.IsSuccessful);
    }
}