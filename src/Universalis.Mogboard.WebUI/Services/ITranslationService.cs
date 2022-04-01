namespace Universalis.Mogboard.WebUI.Services;

public interface ITranslationService
{
    string Translate(string key, string fallback);
}