﻿namespace Workshop.Netcore.WebApi.Models;

public class TodoItem
{
    public long Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public WebApiUser Owner { get; set; }
}
