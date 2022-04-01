using Microsoft.AspNetCore.Http;
using Universalis.Mogboard.WebUI.Translations;

namespace Universalis.Mogboard.WebUI.Services;

internal class TranslationService : ITranslationService
{
    private readonly IHttpContextAccessor _ctxAccessor;

    public TranslationService(IHttpContextAccessor ctxAccessor)
    {
        _ctxAccessor = ctxAccessor;
    }

    public string Translate(string key, string fallback)
    {
        var lang = _ctxAccessor.HttpContext?.Request.Cookies["mogboard_language"] ?? "en";
        return lang switch
        {
            "chs" when TranslationResourceManager.ChineseSimplified.TryGetValue(key, out var termChs) => termChs,
            "ja" when TranslationResourceManager.Japanese.TryGetValue(key, out var termJa) => termJa,
            "fr" when TranslationResourceManager.French.TryGetValue(key, out var termFr) => termFr,
            "de" when TranslationResourceManager.German.TryGetValue(key, out var termDe) => termDe,
            "en" or _ => fallback,
        };
    }
}