using System;
using System.ComponentModel.DataAnnotations;

namespace APIServiceManagement.Application.DTOs.Requests;

public class CancelLeaveDayRequest
{
    [Required]
    public DateTime Date { get; set; }
}
