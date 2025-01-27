using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;

namespace UCGrab.Models
{
    public class AdminDashBoardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int NumberAccounts { get; set; }
        public int NumberStores { get; set; }
        public int NewCustomerInquiries { get; set; }
        public List<Store> RecentStores { get; set; }
        public List<ContactUs> RecentInquiries { get; set; }
        public List<ActivityLog> ActivityLog { get; set; }
    }
}