using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("credit_note_application")]
public class CreditNoteApplication
{
    [Key]
    [Column("application_id")]
    public int ApplicationId { get; set; }

    [Column("credit_note_id")]
    public int CreditNoteId { get; set; }

    [Column("invoice_id")]
    public int InvoiceId { get; set; }

    [Column("applied_amount")]
    public decimal AppliedAmount { get; set; }

    [Column("application_date")]
    public DateTime ApplicationDate { get; set; }

    [Column("bank_account_number")]
    public string? BankAccountNumber { get; set; }

    [Column("bank_name")]
    public string? BankName { get; set; }

    [Column("transaction_reference")]
    public string? TransactionReference { get; set; }

    [Column("applied_by")]
    public Guid AppliedBy { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public CreditNoteMaster CreditNote { get; set; } = null!;
    public InvoiceMaster Invoice { get; set; } = null!;
    public User AppliedByUser { get; set; } = null!;
}
