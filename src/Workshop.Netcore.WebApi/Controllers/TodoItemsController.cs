using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Workshop.Netcore.WebApi.Database;
using Workshop.Netcore.WebApi.Dto;
using Workshop.Netcore.WebApi.Models;

namespace Workshop.Netcore.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TodoItemsController : ControllerBase
    {
        private readonly WebApiDbContext _context;
        private readonly UserManager<WebApiUser> _userManager;

        public TodoItemsController(WebApiDbContext context, UserManager<WebApiUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<WebApiUser> GetCurrentUser()
        {
            var userId = User.Claims.First(t => t.Type == ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId.Value);
            return user!;
        }

        private TodoItemDto ToDto(TodoItem todoItem)
        {
            return new TodoItemDto{
                Id = todoItem.Id,
                IsComplete = todoItem.IsComplete,
                Name = todoItem.Name
            };
        }

        // GET: api/TodoItems
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TodoItemDto>>> GetTodoItems()
        {
            var user = await GetCurrentUser();

            var todoItems = await _context.TodoItems
                .Where(t => t.Owner.Id == user.Id)
                .ToListAsync();

            return todoItems.Select(ToDto).ToList();
        }

        // GET: api/TodoItems/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItemDto>> GetTodoItem(long id)
        {
            var todoItem = await _context.TodoItems
                .Include(e => e.Owner)
                .Where(i => i.Id == id)
                .FirstOrDefaultAsync();

            var user = await GetCurrentUser();

            if (todoItem == null || todoItem.Owner.Id != user.Id)
            {
                return NotFound();
            }

            return ToDto(todoItem);
        }

        // PUT: api/TodoItems/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTodoItem(long id, TodoItemDto todoItem)
        {
            if (id != todoItem.Id)
            {
                return BadRequest();
            }
            
            var existingTodoItem = await _context
                .TodoItems
                .Include(e => e.Owner)
                .Where(i => i.Id == id)
                .FirstOrDefaultAsync();
            
            var user = await GetCurrentUser();

            if (existingTodoItem == null || existingTodoItem.Owner.Id != user.Id)
            {
                return NotFound();
            }

            // Atualiza apenas os campos permitidos
            existingTodoItem.IsComplete = todoItem.IsComplete;
            existingTodoItem.Name = todoItem.Name;
            
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/TodoItems
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<TodoItemDto>> PostTodoItem(TodoItemDto todoItem)
        {
            var userId = User.Claims.First(t => t.Type == ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId.Value);
            
            _context.TodoItems.Add(new TodoItem{
                IsComplete = todoItem.IsComplete,
                Name = todoItem.Name,
                Owner = user!
            });
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItem.Id }, todoItem);
        }

        // DELETE: api/TodoItems/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTodoItem(long id)
        {
            var todoItem = await _context
                .TodoItems
                .Include(e => e.Owner)
                .Where(i => i.Id == id)
                .FirstOrDefaultAsync();

            var user = await GetCurrentUser();

            if (todoItem == null || todoItem.Owner.Id != user.Id)
            {
                return NotFound();
            }

            _context.TodoItems.Remove(todoItem);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TodoItemExists(long id)
        {
            return _context.TodoItems.Any(e => e.Id == id);
        }
    }
}
