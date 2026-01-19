using System;
using System.Text.Json.Serialization;

namespace APIServiceManagement.Application.DTOs.Responses;

public class BannerResponse
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("image_url")]
    public string? ImageUrl { get; set; }

    [JsonPropertyName("background_color")]
    public string? BackgroundColor { get; set; }

    [JsonPropertyName("background_gradient")]
    public string? BackgroundGradient { get; set; }

    [JsonPropertyName("action_url")]
    public string? ActionUrl { get; set; }

    [JsonPropertyName("action_text")]
    public string? ActionText { get; set; }

    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("display_duration_seconds")]
    public int DisplayDurationSeconds { get; set; }

    [JsonPropertyName("start_date")]
    public DateTime? StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateTime? EndDate { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
