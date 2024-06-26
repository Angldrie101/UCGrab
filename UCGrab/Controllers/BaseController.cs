﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UCGrab.Repository;
using UCGrab.Models;
using UCGrab.Database;
using System.Web.Security;

namespace UCGrab.Controllers
{
    public class BaseController : Controller
    {
        public String ErrorMessage;
        public UserManager _userManager;

        public String Username { get { return User.Identity.Name; } }
        public String UserId { get { return _userManager.GetUserByUsername(Username).user_id; } }

        public BaseController()
        {
            ErrorMessage = String.Empty;
            _userManager = new UserManager();

        }
        public void IsUserLoggedSession()
        {
            UserLogged userLogged = new UserLogged();
            if (User != null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    userLogged.UserAccount = _userManager.GetUserByUsername(User.Identity.Name);
                    userLogged.UserInformation = _userManager.CreateOrRetrieve(userLogged.UserAccount.username, ref ErrorMessage);
                }
            }
            Session["User"] = userLogged;
        }
    }
}