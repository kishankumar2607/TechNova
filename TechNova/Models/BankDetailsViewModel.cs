using System.ComponentModel.DataAnnotations;

namespace TechNova.Models
{
    public class BankDetailsViewModel
    {
        [Required]
        public int OrderID { get; set; }

        // purely for display
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Account holder name is required.")]
        public string AccountHolderName { get; set; } = "";

        [Required(ErrorMessage = "Bank name is required.")]
        public string BankName { get; set; } = "";

        [Required(ErrorMessage = "Account number is required.")]
        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; } = "";

        [Display(Name = "Transit/Branch Number")]
        [Required(ErrorMessage = "Transit number is required.")]
        public string TransitNumber { get; set; } = "";

        [Display(Name = "Institution Number")]
        [Required(ErrorMessage = "Institution number is required.")]
        public string InstitutionNumber { get; set; } = "";

        [Display(Name = "SWIFT / IBAN (optional)")]
        public string? SwiftIban { get; set; }
    }
}
