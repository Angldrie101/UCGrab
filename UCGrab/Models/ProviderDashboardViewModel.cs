using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UCGrab.Models
{
    public class ProviderDashboardViewModel
    {
        public decimal TotalSales { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int NewCustomers { get; set; }
        public List<OrderViewModel> RecentOrders { get; set; }
        public List<ProductViewModel> TopSellingProducts { get; set; }
    }
}