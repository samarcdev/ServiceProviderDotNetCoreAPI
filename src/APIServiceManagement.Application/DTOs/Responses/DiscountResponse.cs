using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.DTOs.Responses
{
    public class DiscountResponse
    {
        public bool Success { get; set; }
        public DiscountRequestDto? Discount { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string>? Errors { get; set; }
    }
    public class DiscountRequestDto
    {

        public int DiscountId { get; set; }
        public string? DiscountType { get; set; }
        public decimal? DiscountValue { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }

        public int MinOrderValue { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }



}
