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
    
    public partial class File_Documents
    {
        public int id { get; set; }
        public Nullable<int> user_id { get; set; }
        public string file_document { get; set; }
    
        public virtual User_Accounts User_Accounts { get; set; }
    }
}