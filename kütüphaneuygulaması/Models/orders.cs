using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kütüphaneuygulaması.Models
{
    public class orders
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kitap seçimi zorunludur")]
        [Display(Name = "Kitap")]
        public int bookId { get; set; }

        [Required(ErrorMessage = "Kullanıcı bilgisi zorunludur")]
        [Display(Name = "Kullanıcı")]
        public int userid { get; set; }

        [Required(ErrorMessage = "Miktar zorunludur")]
        [Range(1, int.MaxValue, ErrorMessage = "Miktar 1'den büyük olmalıdır")]
        [Display(Name = "Miktar")]
        public int quantity { get; set; }

        [Required(ErrorMessage = "Sipariş tarihi zorunludur")]
        [Display(Name = "Sipariş Tarihi")]
        [DataType(DataType.DateTime)]
        public DateTime orderdate { get; set; } = DateTime.Now;

        [Display(Name = "Sipariş Durumu")]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Display(Name = "Toplam Fiyat")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [Display(Name = "Teslimat Adresi")]
        [StringLength(500, MinimumLength = 10, ErrorMessage = "Teslimat adresi 10-500 karakter arasında olmalıdır")]
        public string? DeliveryAddress { get; set; }

        [Display(Name = "Telefon")]
        [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Notlar")]
        [StringLength(1000)]
        public string? Notes { get; set; }

        // Audit fields
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation properties
        [Display(Name = "Kitap")]
        public Book? Book { get; set; }

        [Display(Name = "Kullanıcı")]
        public usersaccounts? User { get; set; }

        // Computed properties
        [Display(Name = "Durum Metni")]
        public string StatusText => Status switch
        {
            OrderStatus.Pending => "Beklemede",
            OrderStatus.Confirmed => "Onaylandı",
            OrderStatus.Shipped => "Kargoda",
            OrderStatus.Delivered => "Teslim Edildi",
            OrderStatus.Cancelled => "İptal Edildi",
            _ => "Bilinmiyor"
        };

        [Display(Name = "Durum Rengi")]
        public string StatusColor => Status switch
        {
            OrderStatus.Pending => "warning",
            OrderStatus.Confirmed => "info",
            OrderStatus.Shipped => "primary",
            OrderStatus.Delivered => "success",
            OrderStatus.Cancelled => "danger",
            _ => "secondary"
        };
    }

    public enum OrderStatus
    {
        [Display(Name = "Beklemede")]
        Pending = 0,
        
        [Display(Name = "Onaylandı")]
        Confirmed = 1,
        
        [Display(Name = "Kargoda")]
        Shipped = 2,
        
        [Display(Name = "Teslim Edildi")]
        Delivered = 3,
        
        [Display(Name = "İptal Edildi")]
        Cancelled = 4
    }
}
