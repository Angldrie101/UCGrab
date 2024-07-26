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
          public int TotalStock { get; set; }
    }
}