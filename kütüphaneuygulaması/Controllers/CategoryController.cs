using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;

namespace kütüphaneuygulaması.Controllers
{
    public class CategoryController : Controller
    {
        private readonly kütüphaneuygulamasıContext _context;

        public CategoryController(kütüphaneuygulamasıContext context)
        {
            _context = context;
        }

        // GET: Category
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");
            return View(await _context.Category.ToListAsync());
        }

        // GET: Category/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Category
                .Include(c => c.Books)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            ViewBag.Category = category;
            ViewData["CategoryName"] = category.Name;
            return View(category);
        }

        // GET: Category/Create
        public IActionResult Create()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");
            return View();
        }

        // POST: Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IFormFile file, [Bind("Id,Name,Description")] Category category)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            if (file != null)
            {
                string filename = file.FileName;
                string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images"));
                using (var filestream = new FileStream(Path.Combine(path, filename), FileMode.Create))
                { await file.CopyToAsync(filestream); }

                category.ImageUrl = filename;
            }

            _context.Add(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Category/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Category.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(IFormFile file, int id, [Bind("Id,Name,Description,ImageUrl")] Category category)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            if (file != null)
            {
                string filename = file.FileName;
                string path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot\\images"));
                using (var filestream = new FileStream(Path.Combine(path, filename), FileMode.Create))
                { await file.CopyToAsync(filestream); }

                category.ImageUrl = filename;
            }

            _context.Update(category);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Category/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");
            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.Category
                .FirstOrDefaultAsync(m => m.Id == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: Category/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");
            var category = await _context.Category.FindAsync(id);
            if (category != null)
            {
                _context.Category.Remove(category);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Public category listing
        public async Task<IActionResult> List()
        {
            var categories = await _context.Category.ToListAsync();
            ViewBag.Categories = categories;
            TempData["CategoryCount"] = categories.Count;
            return View(categories);
        }
    }
} 