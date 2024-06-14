using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UCGrab.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
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
        public ActionResult Login(string username, string password, string returnUrl)
        {
          
            return View();
        }

    }
}