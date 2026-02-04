using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("company_configuration")]
public class CompanyConfiguration
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("company_state_id")]
    public int? CompanyStateId { get; set; }

    [Column("company_name")]
    public string? CompanyName { get; set; }

    [Column("company_address_line1")]
    public string? CompanyAddressLine1 { get; set; }

    [Column("company_address_line2")]
    public string? CompanyAddressLine2 { get; set; }

    [Column("company_city")]
    public string? CompanyCity { get; set; }

    [Column("company_pincode")]
    public string? CompanyPincode { get; set; }

    [Column("company_phone")]
    public string? CompanyPhone { get; set; }

    [Column("company_email")]
    public string? CompanyEmail { get; set; }

    [Column("company_gstin")]
    public string? CompanyGstin { get; set; }

    [Column("company_pan")]
    public string? CompanyPan { get; set; }

    [Column("company_logo_url")]
    public string? CompanyLogoUrl { get; set; }

    [Column("company_website")]
    public string? CompanyWebsite { get; set; }

    [Column("payment_terms_days")]
    public int PaymentTermsDays { get; set; } = 30;

    [Column("invoice_prefix")]
    public string InvoicePrefix { get; set; } = "INV";

    [Column("invoice_footer_text")]
    public string? InvoiceFooterText { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public State? CompanyState { get; set; }
}
