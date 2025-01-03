using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;

namespace UCGrab.Models
{
    public class OrderViewModel
    {
        public List<Store> Store { get; set; }
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public List<ProductViewModel> Products { get; set; }
        public decimal? Total { get; set; }
        public int PaymentMethod { get; set; } 
        public string Building { get; set; }
        public string Room { get; set; }
        public string AdditionalInfo { get; set; }
        public int CheckOutOption { get; set; }
        public string StoreImageUrl { get; set; }
        public string DeliveryUserId { get; set; }
        public string Stores { get; set; }
        public string StoreQrCode { get; set; }
        public string Receipt { get; set; }
        public string StoreAddress { get; set; }
        public string GCashReceipt { get; set; }
    }
}