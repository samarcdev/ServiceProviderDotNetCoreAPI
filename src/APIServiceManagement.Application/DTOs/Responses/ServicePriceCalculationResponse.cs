namespace APIServiceManagement.Application.DTOs.Responses;

public sealed class ServicePriceCalculationResponse
{
    public int ServiceId { get; set; }
    public decimal BasePrice { get; set; }
    public decimal PriceRating { get; set; }
    public decimal CalculatedPrice { get; set; }
}
