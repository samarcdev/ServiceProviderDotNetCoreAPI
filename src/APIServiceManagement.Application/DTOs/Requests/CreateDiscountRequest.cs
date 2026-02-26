using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.DTOs.Requests
{
    public class CreateDiscountRequest
    {
        [StringLength(100, ErrorMessage = "Discount name cannot exceed 100 characters")]
        public string? DiscountName { get; set; }
        
        [StringLength(50, ErrorMessage = "Discount type cannot exceed 50 characters")]
        public string? DiscountType { get; set; }
        
        [Range(0, double.MaxValue, ErrorMessage = "Discount value must be non-negative")]
        public decimal? DiscountValue { get; set; }
        
        [Required(ErrorMessage = "Valid from date is required")]
        public DateTime ValidFrom { get; set; }
        
        [Required(ErrorMessage = "Valid to date is required")]
        public DateTime ValidTo { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Minimum order value must be non-negative")]
        public int MinOrderValue { get; set; }
        
        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
