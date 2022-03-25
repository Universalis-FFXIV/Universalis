using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Universalis.Application.Swagger;

public class GroupByNamespaceConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        var controllerNamespace = controller.ControllerType.Namespace;
        if (controllerNamespace == null)
        {
            throw new InvalidOperationException($"Failed to generate documentation: {controller.ControllerName} has no namespace.");
        }
            
        var apiVersion = controllerNamespace.Split(".").Last().ToLower();
        if (!apiVersion.StartsWith("v"))
        {
            apiVersion = "v1";
        }

        controller.ApiExplorer.GroupName = apiVersion;
    }
}