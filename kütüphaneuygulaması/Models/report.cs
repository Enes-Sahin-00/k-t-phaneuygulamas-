using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace kütüphaneuygulaması.Models
{
    public class report
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Müşteri adı zorunludur")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Müşteri adı 2-100 karakter arasında olmalıdır")]
        [Display(Name = "Müşteri Adı")]
        public string customername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Toplam tutar zorunludur")]
        [Range(0, double.MaxValue, ErrorMessage = "Toplam tutar 0'dan büyük olmalıdır")]
        [Display(Name = "Toplam Tutar")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal total { get; set; }

        [Display(Name = "Rapor Tarihi")]
        [DataType(DataType.Date)]
        public DateTime ReportDate { get; set; } = DateTime.Now;

        [Display(Name = "Rapor Türü")]
        public ReportType Type { get; set; } = ReportType.Sales;

        [Display(Name = "Açıklama")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Display(Name = "Dönem Başlangıcı")]
        [DataType(DataType.Date)]
        public DateTime? PeriodStart { get; set; }

        [Display(Name = "Dönem Bitişi")]
        [DataType(DataType.Date)]
        public DateTime? PeriodEnd { get; set; }

        // Audit fields
        [Display(Name = "Oluşturulma Tarihi")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Güncellenme Tarihi")]
        public DateTime? UpdatedDate { get; set; }

        // Computed properties
        [Display(Name = "Toplam Formatı")]
        public string FormattedTotal => $"{total:C}";

        [Display(Name = "Rapor Dönemi")]
        public string PeriodText => PeriodStart.HasValue && PeriodEnd.HasValue 
            ? $"{PeriodStart.Value:dd.MM.yyyy} - {PeriodEnd.Value:dd.MM.yyyy}"
            : "Belirtilmemiş";

        [Display(Name = "Rapor Türü Metni")]
        public string TypeText => Type switch
        {
            ReportType.Sales => "Satış Raporu",
            ReportType.Inventory => "Stok Raporu",
            ReportType.Customer => "Müşteri Raporu",
            ReportType.Revenue => "Gelir Raporu",
            _ => "Bilinmiyor"
        };
    }

    public enum ReportType
    {
        [Display(Name = "Satış Raporu")]
        Sales = 0,
        
        [Display(Name = "Stok Raporu")]
        Inventory = 1,
        
        [Display(Name = "Müşteri Raporu")]
        Customer = 2,
        
        [Display(Name = "Gelir Raporu")]
        Revenue = 3
    }
}
