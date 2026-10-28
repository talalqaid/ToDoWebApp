using System.ComponentModel.DataAnnotations;

namespace TodoWebApp.Models;

public class TodoTask
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter a task title.")]
    [StringLength(120, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [DataType(DataType.Date)]
    public DateTime? DueDate { get; set; }

    public bool IsCompleted { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
