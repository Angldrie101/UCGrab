using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
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

        [HttpPost]
        public ActionResult AddUser(string username, string password, int role_id)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                TempData["ErrorMessage"] = "Username cannot be empty.";
                return RedirectToAction("UserAccounts");
            }

            var userManager = new UserManager();
            
            var newUser = new User_Accounts
            {
                username = username,
                password = password,
                role_id = role_id,
                status = 0, 
            };

            userManager.SignUp(newUser,ref ErrorMessage);

            TempData["SuccessMessage"] = "User added successfully.";
            return RedirectToAction("UserAccounts");
        }

        [AllowAnonymous]
        public JsonResult UserDelete(int id)
        {
            var res = new Response();
            res.code = (Int32)_userManager.DeleteUser(id, ref ErrorMessage);
            res.message = ErrorMessage;

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        [Authorize]
        public ActionResult ManageStore()
        {
            var stores = _storeManager.ListStore();
            return View(stores); 
        }

        [AllowAnonymous]
        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Home");
        }

        [AllowAnonymous]
        public ActionResult ViewUser(int userId)
        {
            var _db = new UCGrabEntities();

            // Fetch user by userId
            var user = _db.User_Accounts
                         .Where(u => u.id == userId)
                         .FirstOrDefault();  // Use FirstOrDefault for single item

            if (user == null)
            {
                return HttpNotFound();  // Better to use HttpNotFound for a 404 response
            }

            // Passing the user object to the view
            return View(user);
        }
    }
}