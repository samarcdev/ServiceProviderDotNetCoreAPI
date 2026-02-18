namespace APIServiceManagement.Application.DTOs.Requests;

public class ApplyCreditNoteRequest
{
    public decimal AppliedAmount { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? TransactionReference { get; set; }
    public string? Notes { get; set; }
}
