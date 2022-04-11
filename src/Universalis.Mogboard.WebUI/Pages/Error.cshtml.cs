using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Universalis.Mogboard.WebUI.Pages
{
    public class ErrorModel : PageModel
    {
        public void OnGet()
        {
            ViewData["Title"] = "Error";
        }
    }
}
