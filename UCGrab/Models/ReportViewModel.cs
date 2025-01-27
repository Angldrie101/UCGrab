using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;

namespace UCGrab.Models
{
    public class ReportViewModel
    {
        public decimal TotalSales { get; set; }
        public List<OrderViewModel> Orders { get; set; }
        public List<ProductViewModel> Products { get; set; }
        public DateTime GeneratedDate { get; set; }
        public string StoreName { get; set; }
    }
}