using System.ComponentModel.DataAnnotations;
using kütüphaneuygulaması.Exceptions;
using kütüphaneuygulaması.Models;
using kütüphaneuygulaması.Models.DTOs;

namespace kütüphaneuygulaması.Services
{
    public interface IValidationService
    {
        bool IsValidEmail(string email);
        bool IsValidPassword(string password);
        bool IsValidUsername(string username);
        string SanitizeInput(string input);
        Task<List<ValidationResult>> ValidateModelAsync(object model);
        bool ValidateBookData(CreateBookDto bookDto);
        Task<CustomValidationResult> ValidateBook(CreateBookDto bookDto);
        Task<CustomValidationResult> ValidateBookUpdate(UpdateBookDto bookDto);
        void ValidateUser(usersaccounts user);
        void ValidateOrder(orders order);
        void ValidateCart(Cart cart);
        bool IsValidISBN(string isbn);
        bool IsValidPhoneNumber(string phoneNumber);
        bool IsValidPrice(decimal price);
        bool IsValidQuantity(int quantity);
        void ThrowValidationException(List<ValidationResult> errors);
    }
} 