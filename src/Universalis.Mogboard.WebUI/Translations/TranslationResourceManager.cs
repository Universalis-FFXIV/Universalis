using System.Reflection;
using System.Text.Json;

namespace Universalis.Mogboard.WebUI.Translations;

internal static class TranslationResourceManager
{
    public static readonly IDictionary<string, string> ChineseSimplified = LoadTranslations("Universalis.Mogboard.WebUI.Translations.Chinese_simplified.json");
    public static readonly IDictionary<string, string> French = LoadTranslations("Universalis.Mogboard.WebUI.Translations.French.json");
    public static readonly IDictionary<string, string> German = LoadTranslations("Universalis.Mogboard.WebUI.Translations.German.json");
    public static readonly IDictionary<string, string> Japanese = LoadTranslations("Universalis.Mogboard.WebUI.Translations.Japanese.json");

    private static IDictionary<string, string> LoadTranslations(string resourceName)
    {
        var data = typeof(TranslationResourceManager).Assembly.GetManifestResourceStream(resourceName)
                   ?? throw new ArgumentException("Unable to load embedded resource.", nameof(resourceName));
        var terms = JsonSerializer.Deserialize<TranslationTerm[]>(data)
                    ?? throw new ArgumentException("Unable to deserialize embedded resource.", nameof(resourceName));
        return terms
            .Where(term => term.Context != null && term.Term != null)
            .ToDictionary(term => term.Context!, term => term.Term!);
    }
}