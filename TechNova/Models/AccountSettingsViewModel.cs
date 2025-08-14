using System.ComponentModel.DataAnnotations;

namespace TechNova.Models
{
    // Profile (name/email) form
    public class AccountSettingsViewModel
    {
        [Required, StringLength(50)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = "";
    }

    // Change password form
    public class ChangePasswordViewModel
    {
        [Required]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = "";

        [Required, MinLength(6)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = "";

        [Required, Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = "";
    }
}