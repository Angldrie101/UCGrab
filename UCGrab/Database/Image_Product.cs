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
    
    public partial class Image_Product
    {
        public int id { get; set; }
        public Nullable<int> product_id { get; set; }
        public string image_file { get; set; }
    
        public virtual Product Product { get; set; }
    }
}
