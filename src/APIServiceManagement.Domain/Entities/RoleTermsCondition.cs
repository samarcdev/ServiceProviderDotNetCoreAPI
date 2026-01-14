namespace APIServiceManagement.Domain.Entities;

public class RoleTermsCondition
{
    public int TermsConditionsId { get; set; }
    public int RoleId { get; set; }

    // Navigation properties
    public TermsAndCondition TermsAndCondition { get; set; }
    public Role Role { get; set; }
}