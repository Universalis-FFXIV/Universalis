using Microsoft.Extensions.DependencyInjection;
using Universalis.Mogboard.WebUI.Services;

namespace Universalis.Mogboard.WebUI;

public static class MogboardWebUIExtensions
{
    public static void AddMogboardWebUI(this IServiceCollection sc)
    {
        sc.AddScoped<ITranslationService, TranslationService>();
    }
}