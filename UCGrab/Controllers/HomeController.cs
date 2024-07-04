using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using UCGrab.Utils;
using UCGrab.Models;
using UCGrab.Database;
using System.IO;

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
                        return RedirectToAction("Index", "Shop");
                    default:
                        return RedirectToAction("Index", "Admin");
                }
            }
            ViewBag.Error = ErrorMessage;

            return View();
        }

        [AllowAnonymous]
        public ActionResult EditProfile()
        {
            IsUserLoggedSession();
            var user = _userManager.Retrieve(User.Identity.Name, ref ErrorMessage);

            if (user == null)
            {
                // Handle the case where user creation or retrieval failed
                // Redirect to an error page or show an error message
                TempData["ErrorMessage"] = "User could not be found or created.";
                return RedirectToAction("Error");
            }

            return View(user);
        }


        [HttpPost]
        public ActionResult EditProfile(User_Information userInf, HttpPostedFileBase profilePicture)
        {
            
            //if(profilePicture != null && profilePicture.ContentLength > 0)
            //{
            //    var fileName = Path.GetFileName(profilePicture.FileName);
            //    var serverSavePath = Path.Combine(Server.MapPath("~/UploadedFiles/"), fileName);
            //    profilePicture.SaveAs(serverSavePath);

            //    var user = _userManager.GetUserInfoByUserId(UserId);

            //    var image = new Image { image_file = fileName, image_id = user.id};

            //    user.Image.Add(image);

            //    if(_userManager.UpdateUserInformation(userInf, ref ErrorMessage) == ErrorCode.Error)
            //    {
            //        ModelState.AddModelError(String.Empty, ErrorMessage);
            //        return View(userInf);
            //    }
            //    else
            //    {
            //        ModelState.AddModelError(String.Empty, "Please select a valid image file.");
                    
            //    }
            //}
            TempData["Message"] = $"User Information {ErrorMessage}!";
            return RedirectToAction("EditProfile");
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

        [AllowAnonymous]
        public ActionResult PageNotFound()
        {
            return View();
        }
    }
}