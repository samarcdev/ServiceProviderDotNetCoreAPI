using APIServiceManagement.Application.DTOs.Requests;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.API.DTOs.Requests;

public class ServiceProviderRegisterFormRequest
{
    [Required]
    public ServiceProviderBasicInfoFormRequest BasicInfo { get; set; }

    [Required]
    public ServiceProviderAddressFormRequest Address { get; set; }

    public ServiceProviderRegisterRequest ToApplicationRequest()
    {
        return new ServiceProviderRegisterRequest
        {
            BasicInfo = new ServiceProviderBasicInfoRequest
            {
                FullName = BasicInfo.FullName,
                Email = BasicInfo.Email,
                PhoneNumber = BasicInfo.PhoneNumber,
                AlternativeMobile = BasicInfo.AlternativeMobile,
                Password = BasicInfo.Password,
                ConfirmPassword = BasicInfo.ConfirmPassword
            },
            Address = new ServiceProviderAddressRequest
            {
                AddressLine1 = Address.AddressLine1,
                AddressLine2 = Address.AddressLine2,
                Street = Address.Street,
                ZipCode = Address.ZipCode,
                CityId = Address.CityId,
                StateId = Address.StateId
            }
        };
    }
}

public class ServiceProviderBasicInfoFormRequest
{
    [Required]
    [StringLength(255, MinimumLength = 2)]
    [RegularExpression("^[a-zA-Z\\s]+$", ErrorMessage = "Full name must contain only letters and spaces.")]
    public string FullName { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; }

    [Required]
    [RegularExpression("^(\\+91)?[6-9]\\d{9}$", ErrorMessage = "Please enter a valid 10-digit phone number.")]
    public string PhoneNumber { get; set; }

    [RegularExpression("^(\\+91)?[6-9]\\d{9}$", ErrorMessage = "Please enter a valid 10-digit phone number.")]
    public string? AlternativeMobile { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).+$", ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, and one number.")]
    public string Password { get; set; }

    [Required]
    public string ConfirmPassword { get; set; }
}

public class ServiceProviderAddressFormRequest
{
    [Required]
    [StringLength(255, MinimumLength = 5)]
    public string AddressLine1 { get; set; }

    [StringLength(255)]
    public string? AddressLine2 { get; set; }

    [StringLength(100)]
    public string? Street { get; set; }

    [Required]
    [RegularExpression("^[1-9][0-9]{5}$", ErrorMessage = "Please enter a valid 6-digit pincode.")]
    public string ZipCode { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a city.")]
    public int CityId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a state.")]
    public int StateId { get; set; }
}
