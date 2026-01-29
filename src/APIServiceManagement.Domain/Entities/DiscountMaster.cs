using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APIServiceManagement.Domain.Entities
{
    [Table("discount_master")]
    public class DiscountMaster
    {
        [Key]
        [Column("discount_id")]
        public int DiscountId { get; set; }
        [Column("discount_name")]
        public string? DiscountName { get; set; }
        [Column("discount_type")]
        public string? DiscountType { get; set; }
        [Column("discount_value")]

        public decimal? DiscountValue { get;set; }
        [Column("valid_from")]
        public DateTime ValidFrom { get; set; }
        [Column("valid_to")]
        public DateTime ValidTo { get; set; }
        [Column("min_order_value")]

        public int MinOrderValue { get; set; }
        [Column("is_active")]
        public bool IsActive { get; set; }
        [Column("created_at")]

        public DateTime CreatedAt { get;set; } = DateTime.UtcNow;

    }
}
