﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UCGrab.Database;
using UCGrab.Repository;

namespace UCGrab.Utils
{
    public enum ErrorCode
    {
        Success,
        Error
    }
    public enum Status
    {
        InActive,
        Active
    }

    public enum RoleType
    {
        Customer,
        Provider,
        Admin
    }

    public enum ProductStatus
    {
        NoStock,
        HasStock
    }

    public enum BookStatus
    {
        Pending,
        Confirmed,
        InProgress,
        Canceled,
        Done
    }

    public class Constant
    {
        public const string Role_Customer = "Customer";
        public const string Role_Provider = "Provider";
        public const string Role_Admin = "Admin";

        public const int ERROR = 1;
        public const int SUCCESS = 0;
    }
    public class Utilities
    {
        public static String gUid
        {
            get
            {
                return Guid.NewGuid().ToString();
            }
        }
        // Return random number for OTP
        public static int code
        {
            get
            {
                Random r = new Random();
                return r.Next(100000, 999999);
            }
        }

        public static List<SelectListItem> ListRole
        {
            get
            {
                BaseRepository<User_Role> role = new BaseRepository<User_Role>();
                var list = new List<SelectListItem>();
                foreach (var item in role.GetAll())
                {
                    var r = new SelectListItem
                    {
                        Text = item.rolename,
                        Value = item.role_id.ToString()
                    };

                    list.Add(r);
                }

                return list;
            }
        }
    }
}