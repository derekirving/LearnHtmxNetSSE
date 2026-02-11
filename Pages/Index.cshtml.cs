using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PageEditingApp.Pages;

public class IndexModel : PageModel
{
    public List<PageInfo> Pages { get; set; } = [];

    public class PageInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public void OnGet()
    {
        // Sample pages - in a real app, these would come from a database
        Pages =
        [
            new PageInfo { Id = "page-1", Title = "Welcome Page", Content = "Welcome to our site!" },
            new PageInfo { Id = "page-2", Title = "About Us", Content = "Learn more about us." },
            new PageInfo { Id = "page-3", Title = "Contact", Content = "Get in touch." },
            new PageInfo { Id = "page-4", Title = "Services", Content = "Our services." },
            new PageInfo { Id = "page-5", Title = "Blog", Content = "Read our blog." }
        ];
    }
}
