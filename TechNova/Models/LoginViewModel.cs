using System.ComponentModel.DataAnnotations;

namespace TechNova.Models
{
    // ViewModel for user login: captures credentials and an optional return URL target after authentication.
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address is required.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Invalid email format.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        [Display(Name = "Password")]
        public string Password { get; set; }
        public string? ReturnUrl { get; set; }
    }
}
