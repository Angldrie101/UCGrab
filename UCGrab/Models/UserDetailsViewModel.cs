﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UCGrab.Models
{
    public class UserDetailsViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string Role { get; set; }
        public string BusinessPermitPath { get; set; }
    }
}