using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UCGrab.Models
{
    public class OrderDeliver
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; }
        public string DeliveryAddress { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string ImgUrl { get; set; }
    }
}