using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class CustomerRegisterRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string Name { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; }

    [Required]
    [Phone]
    [StringLength(20)]
    public string Phone { get; set; }

    [Phone]
    [StringLength(20)]
    public string? AlternativeMobile { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 8)]
    public string Password { get; set; }

    [Required]
    [Compare("Password")]
    public string ConfirmPassword { get; set; }
}
