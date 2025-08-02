using kütüphaneuygulaması.Models;
using BCrypt.Net;

namespace kütüphaneuygulaması.Data
{
    public static class DbInitializer
    {
        public static void Initialize(kütüphaneuygulamasıContext context)
        {
            context.Database.EnsureCreated();

            // Admin kullanıcıları ekle - her zaman oluştur
            var adminUsers = context.usersaccounts.Where(u => u.role == "admin").ToList();
            
            if (adminUsers.Count == 0)
            {
                // Admin hesaplarını oluştur
                var admin1 = new usersaccounts
                {
                    name = "admin",
                    pass = BCrypt.Net.BCrypt.HashPassword("Admin123!"), // Güvenli şifre hashleme
                    email = "admin@admin.com",
                    role = "admin",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                var admin2 = new usersaccounts
                {
                    name = "admin1",
                    pass = BCrypt.Net.BCrypt.HashPassword("admin147"), // Güvenli şifre hashleme
                    email = "admin1@admin.com",
                    role = "admin",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                var admin3 = new usersaccounts
                {
                    name = "testadmin",
                    pass = BCrypt.Net.BCrypt.HashPassword("test123"), // Test admin hesabı
                    email = "testadmin@test.com",
                    role = "admin",
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                context.usersaccounts.AddRange(admin1, admin2, admin3);
                context.SaveChanges();
                
                Console.WriteLine("Admin hesapları oluşturuldu:");
                Console.WriteLine("1. admin / Admin123!");
                Console.WriteLine("2. admin1 / admin147");
                Console.WriteLine("3. testadmin / test123");
            }
            else
            {
                Console.WriteLine($"Mevcut admin hesapları: {adminUsers.Count}");
                foreach (var admin in adminUsers)
                {
                    Console.WriteLine($"- {admin.name} ({admin.email}) - Aktif: {admin.IsActive}");
                }
            }

            // Kategoriler ekle
            if (!context.Category.Any())
            {
                context.Category.AddRange(
                    new Category { Name = "Bilgisayar Bilimi", Description = "Bilgisayar bilimi ve programlama kitapları", ImageUrl = "computer_science.jpg" },
                    new Category { Name = "Bilgisayar Mühendisliği", Description = "Bilgisayar mühendisliği ve teknoloji kitapları", ImageUrl = "computer_engineering.jpg" },
                    new Category { Name = "Roman", Description = "Edebiyat ve roman kitapları", ImageUrl = "novel.jpg" },
                    new Category { Name = "Tarih", Description = "Tarih ve sosyal bilimler kitapları", ImageUrl = "history.jpg" },
                    new Category { Name = "Bilim", Description = "Bilim ve araştırma kitapları", ImageUrl = "science.jpg" }
                );
                context.SaveChanges();
            }

            // Örnek kitap ekle
            if (!context.Book.Any())
            {
                context.Book.AddRange(
                    new Book
                    {
                        title = "C# Programlama",
                        info = "C# programlama dili ile ilgili kapsamlı rehber.",
                        bookquantity = 10,
                        price = 150,
                        cataid = 1,
                        author = "Ahmet Yılmaz",
                        imgfile = "https://cdn1.dokuzsoft.com/u/pelikankitabevi/img/b/u/m/umuttepe-objective-c-programlama-dili-mimar-aslan338fc5c49effd2447e1a7eca9326c04a.jpg"
                    },
                    new Book
                    {
                        title = "Veri Yapıları ve Algoritmalar",
                        info = "Veri yapıları ve algoritma analizi.",
                        bookquantity = 8,
                        price = 200,
                        cataid = 1,
                        author = "Mehmet Demir",
                        imgfile = "https://avatars.mds.yandex.net/i?id=37a40c159f9460310e6c39675a51cf3d7a9cf7c6-5876454-images-thumbs&n=13"
                    },
                    new Book
                    {
                        title = "Web Geliştirme",
                        info = "Modern web geliştirme teknikleri.",
                        bookquantity = 12,
                        price = 180,
                        cataid = 2,
                        author = "Fatma Kaya",
                        imgfile = "https://cdn.kitapsec.com/image/urun/2017/12/06/1512552355.jpg"
                    },
                    new Book
                    {
                        title = "Suç ve Ceza",
                        info = "Klasik Rus edebiyatının başyapıtlarından biri.",
                        bookquantity = 15,
                        price = 45,
                        cataid = 3,
                        author = "Fyodor Dostoyevski",
                        imgfile = "https://images.unsplash.com/photo-1481627834876-b7833e8f5570?w=400&h=600&fit=crop"
                    },
                    new Book
                    {
                        title = "1984",
                        info = "Distopik roman türünün önemli eserlerinden.",
                        bookquantity = 20,
                        price = 35,
                        cataid = 3,
                        author = "George Orwell",
                        imgfile = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=400&h=600&fit=crop"
                    },
                    new Book
                    {
                        title = "Küçük Prens",
                        info = "Çocuklar ve yetişkinler için felsefi masal.",
                        bookquantity = 25,
                        price = 25,
                        cataid = 3,
                        author = "Antoine de Saint-Exupéry",
                        imgfile = "https://images.unsplash.com/photo-1512820790803-83ca734da794?w=400&h=600&fit=crop"
                    }
                );
                context.SaveChanges();
            }
            else
            {
                // Mevcut kitapların resimlerini güncelle
                var booksWithoutImages = context.Book.Where(b => string.IsNullOrEmpty(b.imgfile)).ToList();
                
                foreach (var book in booksWithoutImages)
                {
                    switch (book.title)
                    {
                        case "C# Programlama":
                            book.imgfile = "https://cdn1.dokuzsoft.com/u/pelikankitabevi/img/b/u/m/umuttepe-objective-c-programlama-dili-mimar-aslan338fc5c49effd2447e1a7eca9326c04a.jpg";
                            break;
                        case "Veri Yapıları ve Algoritmalar":
                            book.imgfile = "https://avatars.mds.yandex.net/i?id=37a40c159f9460310e6c39675a51cf3d7a9cf7c6-5876454-images-thumbs&n=13";
                            break;
                        case "Web Geliştirme":
                            book.imgfile = "https://cdn.kitapsec.com/image/urun/2017/12/06/1512552355.jpg";
                            break;
                        default:
                            book.imgfile = "https://images.unsplash.com/photo-1481627834876-b7833e8f5570?w=400&h=600&fit=crop";
                            break;
                    }
                }
                
                if (booksWithoutImages.Any())
                {
                    context.SaveChanges();
                    Console.WriteLine($"{booksWithoutImages.Count} kitabın resmi güncellendi.");
                }
            }
        }
    }
} 