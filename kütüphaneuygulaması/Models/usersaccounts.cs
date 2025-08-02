using System.ComponentModel.DataAnnotations;

namespace kütüphaneuygulaması.Models
{
    public class usersaccounts
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Kullanıcı adı zorunludur")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Kullanıcı adı 3-50 karakter arasında olmalıdır")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "Kullanıcı adı sadece harf, rakam ve alt çizgi içerebilir")]
        public string name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Şifre zorunludur")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter olmalıdır")]
        public string pass { get; set; } = string.Empty;

        [Required(ErrorMessage = "Rol zorunludur")]
        public string role { get; set; } = "customer";

        [Required(ErrorMessage = "Email adresi zorunludur")]
        [EmailAddress(ErrorMessage = "Geçerli bir email adresi giriniz")]
        [StringLength(100, ErrorMessage = "Email adresi 100 karakterden uzun olamaz")]
        public string email { get; set; } = string.Empty;

        // Audit fields
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? UpdatedDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
