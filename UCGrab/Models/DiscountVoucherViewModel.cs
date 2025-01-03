using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;

namespace UCGrab.Models
{
    public class DiscountVoucherViewModel
    {
        public int DiscountId { get; set; }
        public string DiscountType { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public List<Discounts> Discounts { get; set; }
        public List<Vouchers> Vouchers { get; set; }
    }
}