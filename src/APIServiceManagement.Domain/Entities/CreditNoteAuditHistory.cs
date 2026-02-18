using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("credit_note_audit_history")]
public class CreditNoteAuditHistory
{
    [Key]
    [Column("audit_id")]
    public int AuditId { get; set; }

    [Column("credit_note_id")]
    public int CreditNoteId { get; set; }

    [Column("action")]
    public string Action { get; set; } = string.Empty; // Created, Updated, StatusChanged, Cancelled, Applied

    [Column("old_status")]
    public string? OldStatus { get; set; }

    [Column("new_status")]
    public string? NewStatus { get; set; }

    [Column("changed_by")]
    public Guid ChangedBy { get; set; }

    [Column("change_description")]
    public string? ChangeDescription { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public CreditNoteMaster CreditNote { get; set; } = null!;
    public User ChangedByUser { get; set; } = null!;
}
