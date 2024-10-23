using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UCGrab.Database;
using UCGrab.Models;
using UCGrab.Repository;
using UCGrab.Utils;

namespace UCGrab.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        // GET: Admin
        [AllowAnonymous]
        public ActionResult Index()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult UserAccounts()
        {
            if (User.Identity.IsAuthenticated)
            {
                ViewBag.Role = Utilities.ListRole;
            }

            var user = new UserManager();
            var allUsers = user.GetAllBUserInfo();
            return View(allUsers);
        }

        [AllowAnonymous]
        public JsonResult UserDelete(int id)
        {
            var res = new Response();
            res.code = (Int32)_userManager.DeleteUser(id, ref ErrorMessage);
            res.message = ErrorMessage;

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult ManageWallet()
        {
            return View();
        }
        [AllowAnonymous]
        public ActionResult Transaction()
        {
            return View();
        }
        [Authorize]
        public ActionResult ManageStore()
        {
            var stores = _storeManager.ListStore(); // Assuming this method retrieves all stores from the database
            return View(stores); 
        }
    }
}