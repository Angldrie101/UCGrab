using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;

namespace UCGrab.Models
{
    public class UserLogged
    {
        public User_Accounts UserAccount { get; set; }
        public User_Information UserInformation { get; set; }
    }
}