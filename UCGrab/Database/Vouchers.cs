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
    
    public partial class Vouchers
    {
        public int voucher_id { get; set; }
        public Nullable<int> store_id { get; set; }
        public string voucher_code { get; set; }
        public string discount_type { get; set; }
        public Nullable<decimal> discount_value { get; set; }
        public Nullable<decimal> min_order_amount { get; set; }
        public Nullable<int> max_uses { get; set; }
        public Nullable<int> remaining_uses { get; set; }
        public Nullable<System.DateTime> start_date { get; set; }
        public Nullable<System.DateTime> end_date { get; set; }
        public Nullable<int> is_active { get; set; }
    
        public virtual Store Store { get; set; }
    }
}
