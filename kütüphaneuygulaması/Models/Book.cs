using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kütüphaneuygulaması.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kitap adı zorunludur")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Kitap adı 2-200 karakter arasında olmalıdır")]
        [Display(Name = "Kitap Adı")]
        public string title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kitap açıklaması zorunludur")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Kitap açıklaması 10-1000 karakter arasında olmalıdır")]
        [Display(Name = "Açıklama")]
        public string info { get; set; } = string.Empty;

        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı 0'dan büyük olmalıdır")]
        [Display(Name = "Stok Miktarı")]
        public int bookquantity { get; set; }

        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Range(0, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        [Display(Name = "Fiyat")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal price { get; set; }

        [Required(ErrorMessage = "Kategori seçimi zorunludur")]
        [Display(Name = "Kategori")]
        public int cataid { get; set; }

        [Required(ErrorMessage = "Yazar adı zorunludur")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Yazar adı 2-100 karakter arasında olmalıdır")]
        [Display(Name = "Yazar")]
        public string author { get; set; } = string.Empty;

        [Display(Name = "Resim Dosyası")]
        public string? imgfile { get; set; }

        [Display(Name = "ISBN")]
        [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN 10-13 karakter olmalıdır")]
        public string? ISBN { get; set; }

        [Display(Name = "Sayfa Sayısı")]
        [Range(1, int.MaxValue, ErrorMessage = "Sayfa sayısı 1'den büyük olmalıdır")]
        public int? PageCount { get; set; }

        [Display(Name = "Yayın Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? PublicationDate { get; set; }

        [Display(Name = "Dil")]
        [StringLength(50)]
        public string? Language { get; set; } = "Türkçe";

        // Audit fields
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Aktif Mi")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        [Display(Name = "Kategori")]
        public Category? Category { get; set; }

        // Collections
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();
        public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
        public ICollection<orders> Orders { get; set; } = new List<orders>();

        // Computed properties
        [Display(Name = "Stok Durumu")]
        public string StockStatus => bookquantity > 0 ? "Stokta" : "Stokta Yok";

        [Display(Name = "Fiyat Formatı")]
        public string FormattedPrice => $"{price:C}";
    }
}
