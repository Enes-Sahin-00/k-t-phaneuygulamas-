using System.ComponentModel.DataAnnotations;

namespace kütüphaneuygulaması.Models
{
    public class Favorite
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı bilgisi zorunludur")]
        [Display(Name = "Kullanıcı")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Kitap seçimi zorunludur")]
        [Display(Name = "Kitap")]
        public int BookId { get; set; }

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

        // Navigation properties
        [Display(Name = "Kullanıcı")]
        public usersaccounts? User { get; set; }

        [Display(Name = "Kitap")]
        public Book? Book { get; set; }

        // Computed properties
        [Display(Name = "Eklenme Süresi")]
        public string TimeAgo => GetTimeAgo(AddedDate);

        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;
            
            if (timeSpan.TotalDays > 365)
                return $"{(int)(timeSpan.TotalDays / 365)} yıl önce";
            if (timeSpan.TotalDays > 30)
                return $"{(int)(timeSpan.TotalDays / 30)} ay önce";
            if (timeSpan.TotalDays > 1)
                return $"{(int)timeSpan.TotalDays} gün önce";
            if (timeSpan.TotalHours > 1)
                return $"{(int)timeSpan.TotalHours} saat önce";
            if (timeSpan.TotalMinutes > 1)
                return $"{(int)timeSpan.TotalMinutes} dakika önce";
            
            return "Az önce";
        }
    }
} 