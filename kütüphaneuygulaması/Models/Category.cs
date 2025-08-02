using System.ComponentModel.DataAnnotations;

namespace kütüphaneuygulaması.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kategori adı zorunludur")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Kategori adı 2-100 karakter arasında olmalıdır")]
        [Display(Name = "Kategori Adı")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kategori açıklaması zorunludur")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Kategori açıklaması 10-500 karakter arasında olmalıdır")]
        [Display(Name = "Açıklama")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Resim URL")]
        [Url(ErrorMessage = "Geçerli bir URL giriniz")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Renk Kodu")]
        [StringLength(7, MinimumLength = 7, ErrorMessage = "Renk kodu 7 karakter olmalıdır (örn: #FF0000)")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir hex renk kodu giriniz")]
        public string? ColorCode { get; set; } = "#007bff";

        [Display(Name = "Sıralama")]
        [Range(1, int.MaxValue, ErrorMessage = "Sıralama 1'den büyük olmalıdır")]
        public int SortOrder { get; set; } = 1;

        // Audit fields
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Aktif Mi")]
        public bool IsActive { get; set; } = true;
        
        // Navigation property
        [Display(Name = "Kitaplar")]
        public ICollection<Book> Books { get; set; } = new List<Book>();

        // Computed properties
        [Display(Name = "Kitap Sayısı")]
        public int BookCount => Books?.Count ?? 0;

        [Display(Name = "Toplam Stok")]
        public int TotalStock => Books?.Sum(b => b.bookquantity) ?? 0;
    }
} 