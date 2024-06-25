using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using UCGrab.Utils;
using UCGrab.Models;

namespace UCGrab.Controllers
{
    [Authorize(Roles = "Customer")]
    public class HomeController : BaseController
    {
        // GET: Home
        [AllowAnonymous]
        public ActionResult Index()
        {
            IsUserLoggedSession();

            return View();
        }

        [AllowAnonymous]
        public ActionResult Login(String ReturnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index");
            }

            ViewBag.Error = String.Empty;
            ViewBag.ReturnUrl = ReturnUrl;

            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult Login(String username, String password, String ReturnUrl)
        {

            if (_userManager.SignIn(username, password, ref ErrorMessage) == ErrorCode.Success)
            {
                var user = _userManager.GetUserByUsername(username);
                
                FormsAuthentication.SetAuthCookie(username, false);
                //
                if (!String.IsNullOrEmpty(ReturnUrl))
                    return Redirect(ReturnUrl);

                switch (user.User_Role.rolename)
                {
                    case Constant.Role_Customer:
                        return RedirectToAction("Index");
                    case Constant.Role_Provider:
                        return RedirectToAction("Index");
                    default:
                        return RedirectToAction("Index");
                }
            }
            ViewBag.Error = ErrorMessage;

            return View();
        }

        [AllowAnonymous]
        public ActionResult EditProfile()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public ActionResult Shop()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Detail()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Cart()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult FavoritedProduct()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult CheckOut()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Contact()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Ewallet()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult MyOrders()
        {
            return View();
        }
    }
}