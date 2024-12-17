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
        public int NumberInquiries { get; set; }
        public List<User_Accounts> User_Accounts { get; set; }
        public List<Store> Stores { get; set; }
    }
}