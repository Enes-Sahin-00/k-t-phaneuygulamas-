using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using kütüphaneuygulaması.Exceptions;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Models.DTOs;

namespace kütüphaneuygulaması.Services
{
    public class ValidationService : IValidationService
    {
        private static readonly Regex EmailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_]{3,20}$", RegexOptions.Compiled);
        private static readonly Regex PasswordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", RegexOptions.Compiled);
        private static readonly Regex ISBNRegex = new Regex(@"^(?:ISBN(?:-1[03])?:? )?(?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0-9X]{13}$|97[89][0-9]{10}$|(?=(?:[0-9]+[- ]){4})[- 0-9]{17}$)(?:97[89][- ]?)?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X]$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new Regex(@"^[\+]?[1-9][\d]{0,15}$", RegexOptions.Compiled);

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email);
        }

        public bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            // En az 8 karakter, en az bir büyük harf, bir küçük harf, bir rakam
            return password.Length >= 8 &&
                   password.Any(char.IsUpper) &&
                   password.Any(char.IsLower) &&
                   password.Any(char.IsDigit);
        }

        public bool IsValidUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return false;

            return UsernameRegex.IsMatch(username);
        }

        public string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // HTML encoding ve XSS koruması
            return System.Web.HttpUtility.HtmlEncode(input.Trim());
        }

        public async Task<List<ValidationResult>> ValidateModelAsync(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model);
            
            if (!Validator.TryValidateObject(model, validationContext, validationResults, true))
            {
                return validationResults;
            }

            return validationResults;
        }

        public bool ValidateBookData(CreateBookDto bookDto)
        {
            if (string.IsNullOrWhiteSpace(bookDto.Title) || bookDto.Title.Length < 2)
                return false;

            if (string.IsNullOrWhiteSpace(bookDto.Author) || bookDto.Author.Length < 2)
                return false;

            if (string.IsNullOrWhiteSpace(bookDto.Info) || bookDto.Info.Length < 10)
                return false;

            if (bookDto.Price <= 0)
                return false;

            if (bookDto.BookQuantity < 0)
                return false;

            if (bookDto.CategoryId <= 0)
                return false;

            if (!string.IsNullOrWhiteSpace(bookDto.ISBN) && !IsValidISBN(bookDto.ISBN))
                return false;

            if (bookDto.PageCount.HasValue && bookDto.PageCount <= 0)
                return false;

            return true;
        }

        public async Task<CustomValidationResult> ValidateBook(CreateBookDto bookDto)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(bookDto.Title))
                errors.Add("Kitap başlığı gereklidir.");

            if (string.IsNullOrWhiteSpace(bookDto.Author))
                errors.Add("Yazar adı gereklidir.");

            if (bookDto.Price <= 0)
                errors.Add("Fiyat 0'dan büyük olmalıdır.");

            if (bookDto.BookQuantity < 0)
                errors.Add("Stok miktarı negatif olamaz.");

            if (bookDto.CategoryId <= 0)
                errors.Add("Geçerli bir kategori seçilmelidir.");

            return new CustomValidationResult(errors);
        }

        public async Task<CustomValidationResult> ValidateBookUpdate(UpdateBookDto bookDto)
        {
            var errors = new List<string>();

            if (!string.IsNullOrWhiteSpace(bookDto.Title) && bookDto.Title.Length < 3)
                errors.Add("Kitap başlığı en az 3 karakter olmalıdır.");

            if (!string.IsNullOrWhiteSpace(bookDto.Author) && bookDto.Author.Length < 2)
                errors.Add("Yazar adı en az 2 karakter olmalıdır.");

            if (bookDto.Price.HasValue && bookDto.Price <= 0)
                errors.Add("Fiyat 0'dan büyük olmalıdır.");

            if (bookDto.BookQuantity.HasValue && bookDto.BookQuantity < 0)
                errors.Add("Stok miktarı negatif olamaz.");

            return new CustomValidationResult(errors);
        }

        public void ValidateUser(usersaccounts user)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(user.name) || user.name.Length < 3)
                errors.Add("Kullanıcı adı en az 3 karakter olmalıdır.");

            if (string.IsNullOrWhiteSpace(user.email) || !IsValidEmail(user.email))
                errors.Add("Geçerli bir email adresi giriniz.");

            if (string.IsNullOrWhiteSpace(user.pass) || !IsValidPassword(user.pass))
                errors.Add("Şifre en az 8 karakter olmalı ve en az bir büyük harf, bir küçük harf ve bir rakam içermelidir.");

            if (errors.Any())
                throw new System.ComponentModel.DataAnnotations.ValidationException(string.Join("; ", errors));
        }

        public void ValidateOrder(orders order)
        {
            var errors = new List<string>();

            if (order.bookId <= 0)
                errors.Add("Geçerli bir kitap seçilmelidir.");

            if (order.userid <= 0)
                errors.Add("Geçerli bir kullanıcı seçilmelidir.");

            if (order.quantity <= 0)
                errors.Add("Sipariş miktarı 0'dan büyük olmalıdır.");

            if (errors.Any())
                throw new System.ComponentModel.DataAnnotations.ValidationException(string.Join("; ", errors));
        }

        public void ValidateCart(Cart cart)
        {
            var errors = new List<string>();

            if (cart.BookId <= 0)
                errors.Add("Geçerli bir kitap seçilmelidir.");

            if (cart.UserId <= 0)
                errors.Add("Geçerli bir kullanıcı seçilmelidir.");

            if (cart.Quantity <= 0)
                errors.Add("Sepet miktarı 0'dan büyük olmalıdır.");

            if (errors.Any())
                throw new System.ComponentModel.DataAnnotations.ValidationException(string.Join("; ", errors));
        }

        public bool IsValidISBN(string isbn)
        {
            if (string.IsNullOrWhiteSpace(isbn))
                return false;

            return ISBNRegex.IsMatch(isbn);
        }

        public bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            return PhoneRegex.IsMatch(phoneNumber);
        }

        public bool IsValidPrice(decimal price)
        {
            return price >= 0;
        }

        public bool IsValidQuantity(int quantity)
        {
            return quantity >= 0;
        }

        public void ThrowValidationException(List<ValidationResult> errors)
        {
            var errorMessages = errors.Select(e => e.ErrorMessage).ToList();
            throw new System.ComponentModel.DataAnnotations.ValidationException(string.Join("; ", errorMessages));
        }
    }

    public class CustomValidationResult
    {
        public bool IsValid => !Errors.Any();
        public List<string> Errors { get; }

        public CustomValidationResult(List<string> errors)
        {
            Errors = errors;
        }
    }
} 