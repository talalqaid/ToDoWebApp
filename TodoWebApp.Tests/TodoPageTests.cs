using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TodoWebApp.Data;
using TodoWebApp.Models;
using TodoWebApp.Pages;
using Xunit;

namespace TodoWebApp.Tests;

public class TodoPageTests
{
    // Each test owns a fresh SQLite database that exists only in memory.
    private static SqliteTestDb NewDb() => new();

    [Fact]
    public async Task Add_ValidTask_PersistsTitleDescriptionAndDueDate()
    {
        using var fixture = NewDb();
        var page = new IndexModel(fixture.Db)
        {
            Input = new IndexModel.TaskInput
            {
                Title = "  Study C#  ",
                Description = "  Write unit tests  ",
                DueDate = new DateTime(2026, 11, 2)
            }
        };

        var result = await page.OnPostAddAsync();

        Assert.IsType<RedirectToPageResult>(result);
        var saved = await fixture.Db.Tasks.SingleAsync();
        Assert.Equal("Study C#", saved.Title);
        Assert.Equal("Write unit tests", saved.Description);
        Assert.Equal(new DateTime(2026, 11, 2), saved.DueDate);
        Assert.False(saved.IsCompleted);
    }

    [Fact]
    public async Task Add_WhitespaceTitle_DoesNotSaveTask()
    {
        using var fixture = NewDb();
        var page = new IndexModel(fixture.Db)
        {
            Input = new IndexModel.TaskInput { Title = "   " }
        };

        var result = await page.OnPostAddAsync();

        Assert.IsType<PageResult>(result);
        Assert.False(page.ModelState.IsValid);
        Assert.Equal(0, await fixture.Db.Tasks.CountAsync());
    }

    [Fact]
    public async Task Add_InvalidModelState_DoesNotSaveTask()
    {
        using var fixture = NewDb();
        var page = new IndexModel(fixture.Db)
        {
            Input = new IndexModel.TaskInput { Title = "Valid title" }
        };
        // Razor Pages model binding performs validation before calling the handler.
        page.ModelState.AddModelError("Input.Title", "Too long");

        var result = await page.OnPostAddAsync();

        Assert.IsType<PageResult>(result);
        Assert.Empty(await fixture.Db.Tasks.ToListAsync());
    }

    [Fact]
    public async Task Get_AllTasks_ReturnsItemsAndCorrectStatistics()
    {
        using var fixture = NewDb();
        fixture.Db.Tasks.AddRange(
            new TodoTask { Title = "First", IsCompleted = false },
            new TodoTask { Title = "Second", IsCompleted = true });
        await fixture.Db.SaveChangesAsync();
        var page = new IndexModel(fixture.Db);

        await page.OnGetAsync("all");

        Assert.Equal(2, page.Total);
        Assert.Equal(1, page.Completed);
        Assert.Equal(1, page.Remaining);
        Assert.Equal(2, page.Tasks.Count);
    }

    [Theory]
    [InlineData("active", "Open")]
    [InlineData("completed", "Closed")]
    public async Task Get_FiltersTasks(string filter, string expectedTitle)
    {
        using var fixture = NewDb();
        fixture.Db.Tasks.AddRange(
            new TodoTask { Title = "Open", IsCompleted = false },
            new TodoTask { Title = "Closed", IsCompleted = true });
        await fixture.Db.SaveChangesAsync();
        var page = new IndexModel(fixture.Db);

        await page.OnGetAsync(filter);

        Assert.Equal(filter, page.Filter);
        Assert.Equal(expectedTitle, Assert.Single(page.Tasks).Title);
    }

    [Fact]
    public async Task Toggle_ExistingTask_ChangesStatusBothWays()
    {
        using var fixture = NewDb();
        var task = new TodoTask { Title = "Toggle me" };
        fixture.Db.Tasks.Add(task);
        await fixture.Db.SaveChangesAsync();
        var page = new IndexModel(fixture.Db);

        await page.OnPostToggleAsync(task.Id, "active");
        Assert.True((await fixture.Db.Tasks.SingleAsync()).IsCompleted);

        await page.OnPostToggleAsync(task.Id, "active");
        Assert.False((await fixture.Db.Tasks.SingleAsync()).IsCompleted);
    }

    [Fact]
    public async Task Toggle_MissingTask_DoesNotThrow()
    {
        using var fixture = NewDb();
        var page = new IndexModel(fixture.Db);

        var result = await page.OnPostToggleAsync(999, "other");

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Empty(await fixture.Db.Tasks.ToListAsync());
    }

    [Fact]
    public async Task Delete_ExistingTask_RemovesIt()
    {
        using var fixture = NewDb();
        var task = new TodoTask { Title = "Delete me" };
        fixture.Db.Tasks.Add(task);
        await fixture.Db.SaveChangesAsync();
        var page = new IndexModel(fixture.Db);

        var result = await page.OnPostDeleteAsync(task.Id, "all");

        Assert.IsType<RedirectToPageResult>(result);
        Assert.Empty(await fixture.Db.Tasks.ToListAsync());
    }

    [Fact]
    public async Task Delete_MissingTask_DoesNotThrow()
    {
        using var fixture = NewDb();
        var page = new IndexModel(fixture.Db);

        var result = await page.OnPostDeleteAsync(999, "completed");

        Assert.IsType<RedirectToPageResult>(result);
    }

    private sealed class SqliteTestDb : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");
        public TodoDbContext Db { get; }

        public SqliteTestDb()
        {
            _connection.Open();
            var options = new DbContextOptionsBuilder<TodoDbContext>()
                .UseSqlite(_connection)
                .Options;
            Db = new TodoDbContext(options);
            Db.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Db.Dispose();
            _connection.Dispose();
        }
    }
}
