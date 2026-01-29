using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIServiceManagement.Application.DTOs.Requests
{
    public class UpdateDiscountRequest
    {
        public int DiscountId { get; set; }
        public string? DiscountName { get; set; }
        public string? DiscountType { get; set; }

        public decimal? DiscountValue { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }

        public int MinOrderValue { get; set; }
        public bool IsActive { get; set; }

    }
}
