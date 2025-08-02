using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;

namespace kütüphaneuygulaması.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly kütüphaneuygulamasıContext _context;

        public ApiController(kütüphaneuygulamasıContext context)
        {
            _context = context;
        }

        // GET: api/books
        [HttpGet("books")]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            return await _context.Book.Include(b => b.Category).ToListAsync();
        }

        // GET: api/books/5
        [HttpGet("books/{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            var book = await _context.Book.Include(b => b.Category).FirstOrDefaultAsync(b => b.Id == id);
            if (book == null)
            {
                return NotFound();
            }
            return book;
        }

        // POST: api/books
        [HttpPost("books")]
        public async Task<ActionResult<Book>> PostBook(Book book)
        {
            _context.Book.Add(book);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }

        // PUT: api/books/5
        [HttpPut("books/{id}")]
        public async Task<IActionResult> PutBook(int id, Book book)
        {
            if (id != book.Id)
            {
                return BadRequest();
            }
            _context.Entry(book).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/books/5
        [HttpDelete("books/{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var book = await _context.Book.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            _context.Book.Remove(book);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // GET: api/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<Category>>> GetCategories()
        {
            return await _context.Category.Include(c => c.Books).ToListAsync();
        }

        // GET: api/categories/5
        [HttpGet("categories/{id}")]
        public async Task<ActionResult<Category>> GetCategory(int id)
        {
            var category = await _context.Category.Include(c => c.Books).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null)
            {
                return NotFound();
            }
            return category;
        }

        // POST: api/categories
        [HttpPost("categories")]
        public async Task<ActionResult<Category>> PostCategory(Category category)
        {
            _context.Category.Add(category);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, category);
        }

        // GET: api/orders
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<orders>>> GetOrders()
        {
            return await _context.orders.Include(o => _context.Book.FirstOrDefault(b => b.Id == o.bookId)).ToListAsync();
        }

        // POST: api/orders
        [HttpPost("orders")]
        public async Task<ActionResult<orders>> PostOrder(orders order)
        {
            _context.orders.Add(order);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetOrders), new { id = order.Id }, order);
        }

        // GET: api/favorites
        [HttpGet("favorites")]
        public async Task<ActionResult<IEnumerable<Favorite>>> GetFavorites()
        {
            return await _context.Favorite.Include(f => f.Book).Include(f => f.User).ToListAsync();
        }

        // POST: api/favorites
        [HttpPost("favorites")]
        public async Task<ActionResult<Favorite>> PostFavorite(Favorite favorite)
        {
            _context.Favorite.Add(favorite);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFavorites), new { id = favorite.Id }, favorite);
        }

        // DELETE: api/favorites/5
        [HttpDelete("favorites/{id}")]
        public async Task<IActionResult> DeleteFavorite(int id)
        {
            var favorite = await _context.Favorite.FindAsync(id);
            if (favorite == null)
            {
                return NotFound();
            }
            _context.Favorite.Remove(favorite);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
} 