using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UCGrab.Models
{
    public class StoreDiscountedProductsViewModel
    {
        public string StoreName { get; set; }
        public List<ProductViewModel> Products { get; set; }
    }
}