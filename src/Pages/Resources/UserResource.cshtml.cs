using Microsoft.AspNetCore.Mvc.RazorPages;
using Models;

public class UserResourceModel : PageModel
{
    public List<User> Users { get; set; } = new();
    public List<string> TableColumns { get; set; } = new() { "Name", "Email", "CreatedAt", "Actions" };

    public void OnGet()
    {
        // Fetch users from the database or a mock source
        Users = new List<User>
        {
            new User { Id = 1, Name = "John Doe", Email = "john@example.com", CreatedAt = DateTime.Now },
            new User { Id = 2, Name = "Jane Smith", Email = "jane@example.com", CreatedAt = DateTime.Now }
        };
    }
}
