using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PageEditingApp.Services;

namespace PageEditingApp.Pages;

public class EditModel : PageModel
{
    private readonly PageLockService _lockService;

    public EditModel(PageLockService lockService)
    {
        _lockService = lockService;
    }

    public string PageId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public new string Content { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;

    public IActionResult OnGet(string id, string user)
    {
        if (string.IsNullOrEmpty(id))
        {
            return RedirectToPage("/Index");
        }

        PageId = id;
        Username = user ?? "Anonymous";

        // In a real app, fetch from database
        var pages = new Dictionary<string, (string Title, string Content)>
        {
            ["page-1"] = ("Welcome Page", "Welcome to our site!"),
            ["page-2"] = ("About Us", "Learn more about us."),
            ["page-3"] = ("Contact", "Get in touch."),
            ["page-4"] = ("Services", "Our services."),
            ["page-5"] = ("Blog", "Read our blog.")
        };

        if (pages.TryGetValue(id, out var pageData))
        {
            Title = pageData.Title;
            Content = pageData.Content;
        }
        else
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPost(string id, string title, string content)
    {
        // In a real app, save to database
        await Task.CompletedTask;
        
        // Unlock the page
        await _lockService.UnlockPage(id);

        return RedirectToPage("/Index");
    }
}
