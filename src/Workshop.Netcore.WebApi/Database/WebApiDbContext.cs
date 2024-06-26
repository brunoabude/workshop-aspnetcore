﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Workshop.Netcore.WebApi.Models;

namespace Workshop.Netcore.WebApi.Database;

public class WebApiDbContext : IdentityDbContext<WebApiUser>
{
    public WebApiDbContext(DbContextOptions<WebApiDbContext> options) : base(options)
    {
    }

    public DbSet<TodoItem> TodoItems { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<TodoItem>().HasOne(e => e.Owner);
    }
}
