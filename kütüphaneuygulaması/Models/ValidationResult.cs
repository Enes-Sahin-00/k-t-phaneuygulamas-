namespace kütüphaneuygulaması.Models
{
    public class CustomValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public CustomValidationResult()
        {
            IsValid = true;
        }

        public CustomValidationResult(List<string> errors)
        {
            IsValid = !errors.Any();
            Errors = errors;
        }

        public CustomValidationResult(string error)
        {
            IsValid = false;
            Errors = new List<string> { error };
        }
    }
} 