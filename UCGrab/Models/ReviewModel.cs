using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UCGrab.Models
{
    public class ReviewModel
    {
        public int OrderId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? Rating { get; set; }
        public string Comment { get; set; }
        public DateTime? ReviewDate { get; set; }
    }
}