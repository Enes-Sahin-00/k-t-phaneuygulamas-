using System.ComponentModel.DataAnnotations;

namespace kütüphaneuygulaması.Models.DTOs
{
    public class CreateBookDto
    {
        [Required(ErrorMessage = "Kitap adı zorunludur")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Kitap adı 2-200 karakter arasında olmalıdır")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Kitap açıklaması zorunludur")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Kitap açıklaması 10-1000 karakter arasında olmalıdır")]
        public string Info { get; set; } = string.Empty;

        [Required(ErrorMessage = "Stok miktarı zorunludur")]
        [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı 0'dan büyük olmalıdır")]
        public int BookQuantity { get; set; }

        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Range(0, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Kategori seçimi zorunludur")]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "Yazar adı zorunludur")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Yazar adı 2-100 karakter arasında olmalıdır")]
        public string Author { get; set; } = string.Empty;

        [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN 10-13 karakter olmalıdır")]
        public string? ISBN { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sayfa sayısı 1'den büyük olmalıdır")]
        public int? PageCount { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PublicationDate { get; set; }

        [StringLength(50)]
        public string? Language { get; set; } = "Türkçe";
    }

    public class UpdateBookDto
    {
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Kitap adı 2-200 karakter arasında olmalıdır")]
        public string? Title { get; set; }

        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Kitap açıklaması 10-1000 karakter arasında olmalıdır")]
        public string? Info { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stok miktarı 0'dan büyük olmalıdır")]
        public int? BookQuantity { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Fiyat 0'dan büyük olmalıdır")]
        public decimal? Price { get; set; }

        public int? CategoryId { get; set; }

        [StringLength(100, MinimumLength = 2, ErrorMessage = "Yazar adı 2-100 karakter arasında olmalıdır")]
        public string? Author { get; set; }

        [StringLength(13, MinimumLength = 10, ErrorMessage = "ISBN 10-13 karakter olmalıdır")]
        public string? ISBN { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Sayfa sayısı 1'den büyük olmalıdır")]
        public int? PageCount { get; set; }

        [DataType(DataType.Date)]
        public DateTime? PublicationDate { get; set; }

        [StringLength(50)]
        public string? Language { get; set; }
    }
} 