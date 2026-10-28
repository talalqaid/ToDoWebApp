using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TodoWebApp.Data;
using TodoWebApp.Models;

namespace TodoWebApp.Pages;

public class IndexModel(TodoDbContext db) : PageModel
{
    public List<TodoTask> Tasks { get; private set; } = [];
    public string Filter { get; private set; } = "all";
    public int Total { get; private set; }
    public int Completed { get; private set; }
    public int Remaining => Total - Completed;

    [BindProperty]
    public TaskInput Input { get; set; } = new();

    public class TaskInput
    {
        [Required(ErrorMessage = "Enter a task title.")]
        [StringLength(120)]
        public string Title { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }
    }

    public async Task OnGetAsync(string? filter)
    {
        await LoadAsync(filter);
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        Input.Title = Input.Title?.Trim() ?? "";
        Input.Description = Input.Description?.Trim();
        if (string.IsNullOrWhiteSpace(Input.Title))
            ModelState.AddModelError("Input.Title", "Enter a task title.");

        if (!ModelState.IsValid)
        {
            await LoadAsync("all");
            return Page();
        }

        db.Tasks.Add(new TodoTask
        {
            Title = Input.Title,
            Description = Input.Description,
            DueDate = Input.DueDate
        });
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, string? filter)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is not null)
        {
            task.IsCompleted = !task.IsCompleted;
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { filter = ValidFilter(filter) });
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, string? filter)
    {
        var task = await db.Tasks.FindAsync(id);
        if (task is not null)
        {
            db.Tasks.Remove(task);
            await db.SaveChangesAsync();
        }
        return RedirectToPage(new { filter = ValidFilter(filter) });
    }

    private static string ValidFilter(string? filter) => filter is "active" or "completed" ? filter : "all";

    private async Task LoadAsync(string? filter)
    {
        Filter = ValidFilter(filter);
        Total = await db.Tasks.CountAsync();
        Completed = await db.Tasks.CountAsync(t => t.IsCompleted);
        var query = db.Tasks.AsNoTracking().AsQueryable();
        if (Filter == "active") query = query.Where(t => !t.IsCompleted);
        if (Filter == "completed") query = query.Where(t => t.IsCompleted);
        Tasks = await query.OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.DueDate == null)
            .ThenBy(t => t.DueDate)
            .ThenByDescending(t => t.CreatedAtUtc)
            .ToListAsync();
    }
}
