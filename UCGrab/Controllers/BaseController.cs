using System;
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
        public ImageManager _imageManager;
        public StoreManager _storeManager;
        public ProductManager _productManager;
        public CategoryManager _categoryManager;
        public OrderManager _orderManager;

        public String Username { get { return User.Identity.Name; } }
        public String UserId { get { return _userManager.GetUserByUsername(Username).user_id; } }

        public BaseController()
        {
            ErrorMessage = String.Empty;
            _userManager = new UserManager();
            _imageManager = new ImageManager();
            _storeManager = new StoreManager();
            _productManager = new ProductManager();
            _categoryManager = new CategoryManager();
            _orderManager = new OrderManager();

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