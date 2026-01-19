using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities;

[Table("users_addresses")]
public class UsersAddress
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    [Column("user_id")]
    public Guid? UserId { get; set; }
    [Column("street")]
    public string Street { get; set; }
    [Column("zip_code")]
    public string ZipCode { get; set; }
    [Column("city_id")]
    public int? CityId { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; } = false;
    [Column("address_line1")]
    public string AddressLine1 { get; set; }
    [Column("address_line2")]
    public string AddressLine2 { get; set; }
    [Column("state_id")]
    public int? StateId { get; set; }
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; }
    public City City { get; set; }
    public State State { get; set; }
}