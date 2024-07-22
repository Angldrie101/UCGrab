using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UCGrab.Database;
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
        [HttpPost]
        public ActionResult UserAccounts(User_Accounts ua, String ConfirmPass)
        {
            ViewBag.Role = Utilities.ListRole;

            if (!ua.password.Equals(ConfirmPass))
            {
                ModelState.AddModelError(String.Empty, "Password does not match");

                var user = new UserManager();
                var allUsers = user.GetAllBUserInfo();
                return View(allUsers);
            }

            if (_userManager.SignUp(ua, ref ErrorMessage) != ErrorCode.Success)
            {
                ModelState.AddModelError(String.Empty, ErrorMessage);

                var user = new UserManager();
                var allUsers = user.GetAllBUserInfo();
                return View(allUsers);
            }

            string verificationCode = ua.verify_code;
            string emailBody = $"Your verification code is: {verificationCode}";
            string errorMessage = "";

            var mailManager = new MailManager();
            bool emailSent = mailManager.SendEmail(ua.email, "Verification Code", emailBody, ref errorMessage);

            if (!emailSent)
            {
                ModelState.AddModelError(String.Empty, errorMessage);

                var user = new UserManager();
                var allUsers = user.GetAllBUserInfo();
                return View(allUsers);
            }

            TempData["username"] = ua.username;
            return RedirectToAction("UserAccounts");
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
            return View(new Store());
        }
        [HttpPost]
        public ActionResult ManageStore(Store store, string user_id)
        {
            if (_storeManager.AddStoreForUser(store, user_id, ref ErrorMessage) == Utils.ErrorCode.Error)
            {
                ModelState.AddModelError(String.Empty, ErrorMessage);
                return View(store);
            }

            TempData["Message"] = $"Store {store.store_name} added successfully!";
            return View(store);
        }
    }
}