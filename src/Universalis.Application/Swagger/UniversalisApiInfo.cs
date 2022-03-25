using System;
using System.Text;
using Microsoft.OpenApi.Models;

namespace Universalis.Application.Swagger;

public class UniversalisApiInfo : OpenApiInfo
{
    public UniversalisApiInfo()
    {
        Title = "Universalis";
    }

    public UniversalisApiInfo WithDescription(string description)
    {
        Description = description;
        return this;
    }

    public UniversalisApiInfo WithLicense(OpenApiLicense license)
    {
        License = license;
        return this;
    }

    public UniversalisApiInfo WithVersion(Version version)
    {
        var sb = new StringBuilder("v");
        sb.Append(version.Major);

        if (version.Minor != 0)
        {
            sb.Append('.').Append(version.Minor);
        }

        if (version.Build != -1)
        {
            sb.Append('.').Append(version.Build);
        }

        if (version.Revision != -1)
        {
            sb.Append('.').Append(version.Revision);
        }

        Version = sb.ToString();

        return this;
    }
}