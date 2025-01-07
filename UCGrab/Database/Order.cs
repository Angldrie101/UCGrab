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
    
    public partial class Order
    {
        public Order()
        {
            this.Order_Detail = new HashSet<Order_Detail>();
            this.Review = new HashSet<Review>();
        }
    
        public int order_id { get; set; }
        public string user_id { get; set; }
        public Nullable<int> order_status { get; set; }
        public Nullable<int> store_id { get; set; }
        public Nullable<System.DateTime> order_date { get; set; }
        public Nullable<System.DateTime> shipped_date { get; set; }
        public string building { get; set; }
        public string room { get; set; }
        public Nullable<int> payment_method { get; set; }
        public string additional_info { get; set; }
        public Nullable<int> checkOut_option { get; set; }
        public string firstname { get; set; }
        public string lastname { get; set; }
        public string phone { get; set; }
        public string email { get; set; }
        public string delivery_id { get; set; }
        public string gcash_receipt { get; set; }
        public string invoice { get; set; }
    
        public virtual ICollection<Order_Detail> Order_Detail { get; set; }
        public virtual ICollection<Review> Review { get; set; }
        public virtual ICollection<Product> Products { get; set; }

        public virtual Store Store { get; set; }
    }
}
