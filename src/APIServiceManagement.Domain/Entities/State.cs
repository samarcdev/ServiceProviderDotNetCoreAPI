using System.Collections.Generic;

namespace APIServiceManagement.Domain.Entities;

public class State
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Code { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<City> Cities { get; set; }
    public ICollection<AdminStateAssignment> AdminStateAssignments { get; set; }
    public ICollection<UsersAddress> UsersAddresses { get; set; }
}