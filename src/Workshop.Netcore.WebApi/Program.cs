using Microsoft.EntityFrameworkCore;
using Workshop.Netcore.WebApi.Database;
using Microsoft.Extensions.Configuration;
using Workshop.Netcore.WebApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<WebApiDbContext>(opt => 
    opt.UseSqlite(builder.Configuration.GetConnectionString("WebApiLocalDatabase")));

builder.Services.AddIdentityApiEndpoints<WebApiUser>()
.AddEntityFrameworkStores<WebApiDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapGroup("auth").MapIdentityApi<WebApiUser>();

app.MapControllers();
app.Run();
