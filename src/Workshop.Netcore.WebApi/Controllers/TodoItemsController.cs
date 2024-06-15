using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ClosedXML.Excel;
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
        public async Task<ActionResult<IEnumerable<TodoItemDto>>> GetTodoItems([FromQuery] string format = "JSON")
        {
            var user = await GetCurrentUser();

            var todoItems = await _context.TodoItems
                .Where(t => t.Owner.Id == user.Id)
                .ToListAsync();

            if ("JSON".Equals(format?.Trim()?.ToUpperInvariant()))
            {
                return todoItems.Select(ToDto).ToList();
            } else if ("XLSX".Equals(format?.Trim()?.ToUpperInvariant()))
            {
                var stream = new MemoryStream();
                var workBook = new XLWorkbook();
                var workSheet = workBook.Worksheets.Add("Meus todo's");
                var currentRow = 1;

                workSheet.Cell(currentRow, 1).SetValue("Id").Style.Font.Bold = true;
                workSheet.Cell(currentRow, 2).SetValue("Nome").Style.Font.Bold = true;
                workSheet.Cell(currentRow, 3).SetValue("Estado").Style.Font.Bold = true;
                
                currentRow++;

                foreach(var item in todoItems)
                {
                    workSheet.Cell(currentRow, 1).SetValue(item.Id);
                    workSheet.Cell(currentRow, 2).SetValue(item.Name ?? "");
                    workSheet.Cell(currentRow, 3).SetValue(item.IsComplete ? "Completado" : "Pendente");

                    currentRow++;
                }
                
                workSheet.Column(1).AdjustToContents();
                workSheet.Column(2).AdjustToContents();
                workSheet.Column(3).AdjustToContents();

                workBook.SaveAs(stream);
                stream.Seek(0, SeekOrigin.Begin);                
                return File(stream, "application/ms-excel", $"{Guid.NewGuid():N}.xlsx");
            } else
            {
                return BadRequest();
            }
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


        // POST: api/TodoItems/import
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("import")]
        public async Task<ActionResult<IEnumerable<TodoItemDto>>> ImportTodoItemsFromExcel(IFormFile file)
        {
            var userId = User.Claims.First(t => t.Type == ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId.Value);

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var workBook = new XLWorkbook(stream);
            var workSheet = workBook.Worksheets.First();

            var todoItems = workSheet.RowsUsed().Select(row => new TodoItem {
                Name = row.Cell(1).Value.ToString(),
                IsComplete = bool.Parse(row.Cell(2).Value.ToString()),
                Owner = user!
            });

            foreach(var todoItem in todoItems)
            {
                _context.TodoItems.Add(todoItem);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTodoItem), new { id = todoItems.Select(t => t.Id) }, todoItems.Select(ToDto));
        }

        private bool TodoItemExists(long id)
        {
            return _context.TodoItems.Any(e => e.Id == id);
        }
    }
}
