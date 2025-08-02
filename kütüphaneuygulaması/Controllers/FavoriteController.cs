using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Models;

namespace kütüphaneuygulaması.Controllers
{
    public class FavoriteController : Controller
    {
        private readonly kütüphaneuygulamasıContext _context;

        public FavoriteController(kütüphaneuygulamasıContext context)
        {
            _context = context;
        }

        // GET: Favorite
        public async Task<IActionResult> Index()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid"));
            var favorites = await _context.Favorite
                .Where(f => f.UserId == userId)
                .Include(f => f.Book)
                .ToListAsync();

            ViewBag.Favorites = favorites;
            ViewData["FavoriteCount"] = favorites.Count;
            return View(favorites);
        }

        // POST: Favorite/Add
        [HttpPost]
        public async Task<IActionResult> Add(int bookId)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("userid")))
                return RedirectToAction("login", "usersaccounts");

            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid"));
            
            var existingFavorite = await _context.Favorite
                .FirstOrDefaultAsync(f => f.UserId == userId && f.BookId == bookId);

            if (existingFavorite == null)
            {
                _context.Favorite.Add(new Favorite
                {
                    UserId = userId,
                    BookId = bookId,
                    AddedDate = DateTime.Now
                });
                await _context.SaveChangesAsync();
                TempData["Message"] = "Kitap favorilere eklendi!";
            }
            else
            {
                TempData["Message"] = "Bu kitap zaten favorilerinizde!";
            }

            return RedirectToAction("catalogue", "Books");
        }

        // POST: Favorite/Remove
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var favorite = await _context.Favorite.FindAsync(id);
            if (favorite != null)
            {
                _context.Favorite.Remove(favorite);
                await _context.SaveChangesAsync();
                TempData["Message"] = "Kitap favorilerden kaldırıldı!";
            }
            return RedirectToAction("Index");
        }

        // Admin: View all favorites
        public async Task<IActionResult> AdminIndex()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            var favorites = await _context.Favorite
                .Include(f => f.Book)
                .Include(f => f.User)
                .ToListAsync();

            ViewBag.Favorites = favorites;
            return View(favorites);
        }
    }
} 