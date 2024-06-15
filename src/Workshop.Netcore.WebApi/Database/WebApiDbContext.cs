using Microsoft.EntityFrameworkCore;
using Workshop.Netcore.WebApi.Models;

namespace Workshop.Netcore.WebApi.Database;

public class WebApiDbContext : DbContext
{
    public WebApiDbContext(DbContextOptions<WebApiDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; } = null!;
}
