using kütüphaneuygulaması.Data;
using kütüphaneuygulaması.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace kütüphaneuygulaması.Models
{
    public class usersaccountsController : Controller
    {
        private readonly kütüphaneuygulamasıContext _context;
        private readonly IPasswordService _passwordService;
        private readonly IValidationService _validationService;

        public usersaccountsController(kütüphaneuygulamasıContext context, IPasswordService passwordService, IValidationService validationService)
        {
            _context = context;
            _passwordService = passwordService;
            _validationService = validationService;
        }

        // GET: usersaccounts
        public async Task<IActionResult> Index()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");
            return View(await _context.usersaccounts.ToListAsync());
        }

        // GET: usersaccounts/Create
        public IActionResult Create()
        {
            return View();
        }

        //  usersaccounts/login
        public IActionResult login()
        {
            return View();
        }

        [HttpPost, ActionName("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> login(string na, string pa)
        {
            if (string.IsNullOrEmpty(na) || string.IsNullOrEmpty(pa))
            {
                ViewData["Message"] = "Kullanıcı adı ve şifre gereklidir";
                return View();
            }

            // Input sanitization
            na = _validationService.SanitizeInput(na);
            pa = _validationService.SanitizeInput(pa);

            var user = await _context.usersaccounts.FirstOrDefaultAsync(u => u.name == na && u.IsActive);
            if (user != null && _passwordService.VerifyPassword(pa, user.pass))
            {
                HttpContext.Session.SetString("Name", user.name);
                HttpContext.Session.SetString("Role", user.role);
                HttpContext.Session.SetString("userid", user.Id.ToString());
                
                if (user.role == "customer")
                    return RedirectToAction("catalogue", "books");
                else
                    return RedirectToAction("Index", "books");
            }
            
            ViewData["Message"] = "Kullanıcı adı veya şifre hatalı";
            return View();
        }

        // POST: usersaccounts/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,name,pass,email")] usersaccounts usersaccounts)
        {
            if (ModelState.IsValid)
            {
                // Validation
                if (!_validationService.IsValidUsername(usersaccounts.name))
                {
                    ModelState.AddModelError("name", "Geçersiz kullanıcı adı formatı");
                    return View(usersaccounts);
                }

                if (!_validationService.IsValidEmail(usersaccounts.email))
                {
                    ModelState.AddModelError("email", "Geçersiz email formatı");
                    return View(usersaccounts);
                }

                if (!_validationService.IsValidPassword(usersaccounts.pass))
                {
                    ModelState.AddModelError("pass", "Şifre en az 8 karakter olmalı ve büyük harf, küçük harf ve rakam içermelidir");
                    return View(usersaccounts);
                }

                // Check if username already exists
                if (await _context.usersaccounts.AnyAsync(u => u.name == usersaccounts.name))
                {
                    ModelState.AddModelError("name", "Bu kullanıcı adı zaten kullanılıyor");
                    return View(usersaccounts);
                }

                // Check if email already exists
                if (await _context.usersaccounts.AnyAsync(u => u.email == usersaccounts.email))
                {
                    ModelState.AddModelError("email", "Bu email adresi zaten kullanılıyor");
                    return View(usersaccounts);
                }

                // Hash password
                usersaccounts.pass = _passwordService.HashPassword(usersaccounts.pass);
                usersaccounts.role = "customer";
                usersaccounts.CreatedDate = DateTime.Now;
                usersaccounts.IsActive = true;

                _context.Add(usersaccounts);
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Kayıt başarılı! Giriş yapabilirsiniz.";
                return RedirectToAction(nameof(login));
            }
            
            return View(usersaccounts);
        }

        // GET: usersaccounts/CreateAdmin
        public IActionResult CreateAdmin()
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin([Bind("Id,name,pass,email")] usersaccounts usersaccounts)
        {
            if (HttpContext.Session.GetString("Role") != "admin")
                return RedirectToAction("login", "usersaccounts");

            if (ModelState.IsValid)
            {
                // Validation
                if (!_validationService.IsValidUsername(usersaccounts.name))
                {
                    ModelState.AddModelError("name", "Geçersiz kullanıcı adı formatı");
                    return View(usersaccounts);
                }

                if (!_validationService.IsValidEmail(usersaccounts.email))
                {
                    ModelState.AddModelError("email", "Geçersiz email formatı");
                    return View(usersaccounts);
                }

                if (!_validationService.IsValidPassword(usersaccounts.pass))
                {
                    ModelState.AddModelError("pass", "Şifre en az 8 karakter olmalı ve büyük harf, küçük harf ve rakam içermelidir");
                    return View(usersaccounts);
                }

                // Check if username already exists
                if (await _context.usersaccounts.AnyAsync(u => u.name == usersaccounts.name))
                {
                    ModelState.AddModelError("name", "Bu kullanıcı adı zaten kullanılıyor");
                    return View(usersaccounts);
                }

                // Check if email already exists
                if (await _context.usersaccounts.AnyAsync(u => u.email == usersaccounts.email))
                {
                    ModelState.AddModelError("email", "Bu email adresi zaten kullanılıyor");
                    return View(usersaccounts);
                }

                // Hash password
                usersaccounts.pass = _passwordService.HashPassword(usersaccounts.pass);
                usersaccounts.role = "admin";
                usersaccounts.CreatedDate = DateTime.Now;
                usersaccounts.IsActive = true;

                _context.Add(usersaccounts);
                await _context.SaveChangesAsync();
                
                TempData["Message"] = "Admin kullanıcısı başarıyla oluşturuldu.";
                return RedirectToAction("Index");
            }
            
            return View(usersaccounts);
        }

        // GET: usersaccounts/Edit/5
        public async Task<IActionResult> Edit()
        {
            // Kullanıcı sadece kendi profilini düzenleyebilir, admin ise herkesi düzenleyebilir
            if (HttpContext.Session.GetString("Role") != "admin" && HttpContext.Session.GetString("userid") == null)
                return RedirectToAction("login", "usersaccounts");
            int id = Convert.ToInt32(HttpContext.Session.GetString("userid"));

            var usersaccounts = await _context.usersaccounts.FindAsync(id);
            if (usersaccounts == null)
            {
                return NotFound();
            }
          
            return View(usersaccounts);
        }

        // POST: usersaccounts/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,name,pass,role,email")] usersaccounts usersaccounts)
        {
            // Kullanıcı sadece kendi profilini düzenleyebilir, admin ise herkesi düzenleyebilir
            if (HttpContext.Session.GetString("Role") != "admin" && HttpContext.Session.GetString("userid") != id.ToString())
                return RedirectToAction("login", "usersaccounts");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingUser = await _context.usersaccounts.FindAsync(id);
                    if (existingUser == null)
                    {
                        return NotFound();
                    }

                    // Validation
                    if (!_validationService.IsValidUsername(usersaccounts.name))
                    {
                        ModelState.AddModelError("name", "Geçersiz kullanıcı adı formatı");
                        return View(usersaccounts);
                    }

                    if (!_validationService.IsValidEmail(usersaccounts.email))
                    {
                        ModelState.AddModelError("email", "Geçersiz email formatı");
                        return View(usersaccounts);
                    }

                    // Check if username already exists (excluding current user)
                    if (await _context.usersaccounts.AnyAsync(u => u.name == usersaccounts.name && u.Id != id))
                    {
                        ModelState.AddModelError("name", "Bu kullanıcı adı zaten kullanılıyor");
                        return View(usersaccounts);
                    }

                    // Check if email already exists (excluding current user)
                    if (await _context.usersaccounts.AnyAsync(u => u.email == usersaccounts.email && u.Id != id))
                    {
                        ModelState.AddModelError("email", "Bu email adresi zaten kullanılıyor");
                        return View(usersaccounts);
                    }

                    // Update fields
                    existingUser.name = usersaccounts.name;
                    existingUser.email = usersaccounts.email;
                    existingUser.UpdatedDate = DateTime.Now;

                    // Only hash password if it's changed
                    if (!string.IsNullOrEmpty(usersaccounts.pass) && usersaccounts.pass != existingUser.pass)
                    {
                        if (!_validationService.IsValidPassword(usersaccounts.pass))
                        {
                            ModelState.AddModelError("pass", "Şifre en az 8 karakter olmalı ve büyük harf, küçük harf ve rakam içermelidir");
                            return View(usersaccounts);
                        }
                        existingUser.pass = _passwordService.HashPassword(usersaccounts.pass);
                    }

                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                    
                    TempData["Message"] = "Profil başarıyla güncellendi.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!usersaccountsExists(usersaccounts.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(login));
            }
            return View(usersaccounts);
        }
      
        private bool usersaccountsExists(int id)
        {
            return _context.usersaccounts.Any(e => e.Id == id);
        }

        // GET: usersaccounts/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("login");
        }
    }
}
