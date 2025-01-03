﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using UCGrab.Database;
using UCGrab.Models;
using UCGrab.Repository;
using UCGrab.Utils;
using ImageMagick;
using System.IO;

namespace UCGrab.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseController
    {
        // GET: Admin
        [AllowAnonymous]
        public ActionResult Index()
        {
            var _db = new UCGrabEntities();

            var totalRevenue = 0;
            var totalStores = _db.Store.Count();
            var totalUsers = _db.User_Accounts.Count();
            var customerInquiries = _db.ContactUs.Count();

            // Fetch the 5 most recent stores
            var recentStores = _db.Store
                .OrderByDescending(s => s.store_id) // Assuming 'CreatedAt' column exists
                .Take(5)
                .ToList();

            // Fetch the 5 most recent customer inquiries
            var inquiries = _db.ContactUs
                .OrderByDescending(c => c.contact_id) // Assuming 'CreatedAt' column exists
                .Take(5)
                .ToList();

            var dashboardData = new AdminDashBoardViewModel
            {
                TotalRevenue = totalRevenue,
                NumberStores = totalStores,
                NumberAccounts = totalUsers,
                NewCustomerInquiries = customerInquiries,
                RecentStores = recentStores,
                RecentInquiries = inquiries
            };

            return View(dashboardData);
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

            userManager.SignUp(newUser, ref ErrorMessage);

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
            var stores = _storeManager.ManageStore();
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

            var user = _db.User_Accounts
                          .Where(u => u.id == userId)
                          .FirstOrDefault();

            if (user == null)
            {
                return HttpNotFound();
            }

            var businessPermit = _db.File_Documents
                                    .Where(fd => fd.user_id == userId)
                                    .Select(fd => fd.file_document)
                                    .FirstOrDefault();

            var userViewModel = new UserDetailsViewModel
            {
                UserId = user.id,
                Username = user.username,
                Email = user.email,
                Status = user.status == 1 ? "Active" : "Inactive",
                Role = user.User_Role?.rolename,
                BusinessPermitPath = businessPermit
            };

            return View(userViewModel);
        }

        [HttpPost]
        public ActionResult AcceptUser(int userId)
        {
            var _db = new UCGrabEntities();

            var user = _db.User_Accounts.FirstOrDefault(u => u.id == userId);
            if (user == null)
            {
                return HttpNotFound();
            }

            user.status = (int)Status.Accepted;

            _db.SaveChanges();

            string verificationCode = user.verify_code;

            string emailBody = $"Your verification code is: {verificationCode}";
            string errorMessage = "";

            var mailManager = new MailManager();
            bool emailSent = mailManager.SendEmail(user.email, "Verification Code", emailBody, ref errorMessage);

            if (!emailSent)
            {
                ModelState.AddModelError(string.Empty, $"Failed to send email: {errorMessage}");
                return RedirectToAction("UserAccounts", "Admin");
            }

            return RedirectToAction("UserAccounts", "Admin");
        }
        [HttpPost]
        public ActionResult RejectUser(int userId)
        {
            var _db = new UCGrabEntities();

            var user = _db.User_Accounts.FirstOrDefault(u => u.id == userId);
            if (user == null)
            {
                return HttpNotFound();
            }

            user.status = (int)Status.Rejected;

            _db.SaveChanges();
            

            return RedirectToAction("UserAccounts", "Admin");
        }
        public string ConvertPdfToImage(string pdfPath)
        {
            string imagePath = Path.ChangeExtension(pdfPath, ".png");
            using (var images = new MagickImageCollection(pdfPath))
            {
                var image = images[0];
                image.Write(imagePath);
            }
            return imagePath;
        }

        [AllowAnonymous]
        public ActionResult Inquires()
        {
            return View();
        }
    }
}
