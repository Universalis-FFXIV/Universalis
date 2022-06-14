using System;

namespace Universalis.Application.Swagger;

[AttributeUsage(AttributeTargets.Method)]
public sealed class ApiTagAttribute : Attribute
{
    public string Tag { get; }

    public ApiTagAttribute(string tag)
    {
        Tag = tag;
    }
}