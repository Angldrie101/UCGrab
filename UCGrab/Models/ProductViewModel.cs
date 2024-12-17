using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;

namespace UCGrab.Models
{
    public class ProductViewModel
    {

        public Product Product { get; set; }

        public Order Order { get; set; }

        public Store Store { get; set; }

        public Category Category { get; set; }

        public int TotalStock { get; set; }

        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public string ImageFilePath { get; set; }

        public decimal Total { get; set; }
        
        public int ProductId { get; set; }
    }
}