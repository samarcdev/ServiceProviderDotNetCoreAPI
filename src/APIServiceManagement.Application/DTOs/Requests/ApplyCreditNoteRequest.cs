using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class ApplyCreditNoteRequest
{
    [Required(ErrorMessage = "Applied amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Applied amount must be greater than 0")]
    public decimal AppliedAmount { get; set; }
    
    [StringLength(50, ErrorMessage = "Bank account number cannot exceed 50 characters")]
    public string? BankAccountNumber { get; set; }
    
    [StringLength(100, ErrorMessage = "Bank name cannot exceed 100 characters")]
    public string? BankName { get; set; }
    
    [StringLength(100, ErrorMessage = "Transaction reference cannot exceed 100 characters")]
    public string? TransactionReference { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? Notes { get; set; }
}
