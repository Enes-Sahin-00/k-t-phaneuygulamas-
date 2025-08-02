using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kütüphaneuygulaması.Models
{
    public class Cart
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı bilgisi zorunludur")]
        [Display(Name = "Kullanıcı")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Kitap seçimi zorunludur")]
        [Display(Name = "Kitap")]
        public int BookId { get; set; }

        [Required(ErrorMessage = "Miktar zorunludur")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar 1'den büyük olmalıdır")]
        [Display(Name = "Miktar")]
        public int Quantity { get; set; }

        [Required(ErrorMessage = "Eklenme tarihi zorunludur")]
        [Display(Name = "Eklenme Tarihi")]
        [DataType(DataType.DateTime)]
        public DateTime AddedDate { get; set; } = DateTime.Now;

        [Display(Name = "Notlar")]
        [StringLength(500)]
        public string? Notes { get; set; }

        // Audit fields
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation property
        [Display(Name = "Kitap")]
        public Book? Book { get; set; }

        [Display(Name = "Kullanıcı")]
        public usersaccounts? User { get; set; }

        // Computed properties
        [Display(Name = "Toplam Fiyat")]
        public decimal TotalPrice => Book?.price * Quantity ?? 0;

        [Display(Name = "Toplam Fiyat Formatı")]
        public string FormattedTotalPrice => $"{TotalPrice:C}";

        [Display(Name = "Stok Kontrolü")]
        public bool IsInStock => Book?.bookquantity >= Quantity;

        [Display(Name = "Stok Durumu")]
        public string StockStatus => IsInStock ? "Stokta" : "Yetersiz Stok";
    }
} 