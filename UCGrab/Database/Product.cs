//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace UCGrab.Database
{
    using System;
    using System.Collections.Generic;
    
    public partial class Product
    {
        public Product()
        {
            this.Discounts = new HashSet<Discounts>();
            this.Favorites = new HashSet<Favorites>();
            this.Image_Product = new HashSet<Image_Product>();
            this.Order_Detail = new HashSet<Order_Detail>();
            this.Stock = new HashSet<Stock>();
        }
    
        public int id { get; set; }
        public string product_id { get; set; }
        public string product_name { get; set; }
        public string product_description { get; set; }
        public int category_id { get; set; }
        public string size { get; set; }
        public decimal price { get; set; }
        public Nullable<System.DateTime> date_created { get; set; }
        public int status { get; set; }
        public string user_id { get; set; }
        public Nullable<int> store_id { get; set; }
    
        public virtual Category Category { get; set; }
        public virtual ICollection<Discounts> Discounts { get; set; }
        public virtual ICollection<Favorites> Favorites { get; set; }
        public virtual ICollection<Image_Product> Image_Product { get; set; }
        public virtual ICollection<Order_Detail> Order_Detail { get; set; }
        public virtual Store Store { get; set; }
        public virtual ICollection<Stock> Stock { get; set; }
    }
}
