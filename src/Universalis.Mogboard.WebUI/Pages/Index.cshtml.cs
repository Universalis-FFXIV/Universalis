using Microsoft.AspNetCore.Mvc.RazorPages;
using Universalis.Mogboard.WebUI.Services;

namespace Universalis.Mogboard.WebUI.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ITranslationService _localize;

        public IndexModel(ITranslationService localize)
        {
            _localize = localize;
        }

        public void OnGet()
        {
            ViewData["Title"] = _localize.Translate("generic_index", "Index");
        }
    }
}
