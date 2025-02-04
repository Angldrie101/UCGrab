﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using UCGrab.Utils;
using UCGrab.Models;
using UCGrab.Database;
using System.IO;
using UCGrab.Repository;
using System.Data.Entity;
using iTextSharp.text;
using iTextSharp.text.pdf;
using DbImage = UCGrab.Database.Image;
using PdfImage = iTextSharp.text.Image;
using System.Text;

namespace UCGrab.Controllers
{
    [Authorize(Roles = "Customer")]
    public class HomeController : BaseController
    {
        // GET: Home
        [Authorize]
        public ActionResult Index()
        {
            IsUserLoggedSession();

            using (var db = new UCGrabEntities())
            {
                var shops = db.Store
                              .Where(s => s.status == 1)
                              .Include(s => s.Image_Store) 
                              .ToList();

                var products = db.Product.OrderByDescending(p => p.date_created)
                                 .Where(p => p.status == 1)
                                 .Include(p => p.Image_Product)
                                 .ToList();

                ViewBag.Shops = shops ?? new List<Store>();
                ViewBag.Products = products ?? new List<Product>();
            }

            return View();
        }
        [AllowAnonymous]
        public ActionResult SelectRole()
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
        public ActionResult Login(String username, String password, String ReturnUrl)
        {
            if (_userManager.SignIn(username, password, ref ErrorMessage) == ErrorCode.Success)
            {
                var user = _userManager.GetUserByUsername(username);
                var info = _userManager.CreateOrRetrieve(username, ref ErrorMessage);

                if (user.email == null)
                {
                    TempData["username"] = username;
                    return RedirectToAction("ActivateAccount");
                }
                else if (user.status != (int)Status.Active)
                {
                    TempData["username"] = username;
                    return RedirectToAction("Verify");
                }

                FormsAuthentication.SetAuthCookie(username, false);

                if (!string.IsNullOrEmpty(ReturnUrl))
                    return Redirect(ReturnUrl);

                switch (user.User_Role.rolename)
                {
                    case Constant.Role_Customer:
                        if (info.first_name == null)
                        {
                            return RedirectToAction("EditProfile");
                        }
                        else
                        {
                            return RedirectToAction("Index");
                        }

                    case Constant.Role_Provider:
                        return RedirectToAction("Index", "Shop");

                    case Constant.Role_DeliveryMan:
                        return RedirectToAction("Orders", "Delivery");

                    default:
                        return RedirectToAction("Index", "Admin");
                }
            }

            ViewBag.Error = ErrorMessage;
            return View();
        }

        [AllowAnonymous]
        public ActionResult ActivateAccount()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ActivateAccount(string username, string email, string password)
        {
            var user = _userManager.GetUserByUsername(username);

            if (user != null)
            {
                user.email = email;
                user.password = password;

                User_Accounts updatedUser = new User_Accounts
                {
                    id = user.id,
                    user_id = user.user_id,
                    username = user.username,
                    role_id = user.role_id,
                    status = user.status,
                    verify_code = user.verify_code,
                    date_created = user.date_created,
                    email = email,
                    password = password
                };

                string errMsg = string.Empty;
                var result = _userManager.UpdateUser(updatedUser, ref errMsg);

                if (result == ErrorCode.Success)
                {
                    string verificationCode = user.verify_code;

                    string emailBody = $"Your verification code is: {verificationCode}";
                    string errorMessage = "";

                    var mailManager = new MailManager();
                    bool emailSent = mailManager.SendEmail(email, "Verification Code", emailBody, ref errorMessage);

                    if (!emailSent)
                    {
                        ModelState.AddModelError(String.Empty, errorMessage);
                        return View();
                    }
                    TempData["username"] = updatedUser.username;

                    return RedirectToAction("Verify");
                }
                else
                {
                    TempData["error"] = "Failed to update the user details. " + errMsg;
                }
            }
            TempData["error"] = "User not found or invalid details.";
            return View();
        }
        private string HashPassword(string password)
        {
            return password; 
        }

        [AllowAnonymous]
        public ActionResult ChangePass()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult ChangePass(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                TempData["error"] = "Username is required.";
                return View();
            }

            var user = _userManager.GetUserByUsername(username);

            if (user == null)
            {
                TempData["error"] = "User not found. Please ensure the username is correct.";
                return View();
            }

            user.password = password;

            User_Accounts updatedUser = new User_Accounts
            {
                id = user.id,
                user_id = user.user_id,
                username = user.username,
                role_id = user.role_id,
                status = user.status,
                verify_code = user.verify_code,
                date_created = user.date_created,
                email = user.email, 
                password = password
            };

            string errMsg = string.Empty;
            var result = _userManager.UpdateUser(updatedUser, ref errMsg);

            if (result == ErrorCode.Success)
            {
                string verificationCode = user.verify_code;

                string emailBody = $"Your verification code is: {verificationCode}";
                string errorMessage = "";

                var mailManager = new MailManager();
                bool emailSent = mailManager.SendEmail(user.email, "Verification Code", emailBody, ref errorMessage);

                if (!emailSent)
                {
                    ModelState.AddModelError(string.Empty, errorMessage);
                    return View();
                }
                TempData["username"] = updatedUser.username;

                return RedirectToAction("Verify");
            }
            else
            {
                TempData["error"] = "Failed to update the password. " + errMsg;
            }

            return View();
        }

        [AllowAnonymous]
        public ActionResult Verify()
        {
            if (string.IsNullOrEmpty(TempData["username"] as string))
                return RedirectToAction("Index");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Verify(string verify_code, string username)
        {
            if (String.IsNullOrEmpty(username))
                return RedirectToAction("Index");

            TempData["username"] = username;

            var user = _userManager.GetUserByUsername(username);
            if (user == null)
            {
                TempData["error"] = "User not found.";
            }
            if (!user.verify_code.Equals(verify_code))
            {
                TempData["error"] = "Incorrect Code";
                return View();
            }

            user.status = (Int32)Status.Active;
            _userManager.UpdateUser(user, ref ErrorMessage);


            SendActivationNotificationEmail(user.email);
            
            return RedirectToAction("Index");
        }

        private void SendActivationNotificationEmail(string userEmail)
        {
            string emailBody = "Your account has been activated successfully.";
            string errorMessage = "";

            var mailManager = new MailManager();
            bool emailSent = mailManager.SendEmail(userEmail, "Account Activation Notification", emailBody, ref errorMessage);

            if (!emailSent)
            {
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult ResendCode(User_Accounts ua, string username)
        {
            if (String.IsNullOrEmpty(username))
                return RedirectToAction("Login");

            var user = _userManager.GetUserByUsername(username);

            string verificationCode = ua.verify_code;
            string emailBody = $"Your verification code is: {verificationCode}";
            string errorMessage = "";

            var mailManager = new MailManager();
            bool emailSent = mailManager.SendEmail(ua.email, "Verification Code", emailBody, ref errorMessage);

            if (!emailSent)
            {
                ModelState.AddModelError(String.Empty, errorMessage);
                return View(ua);
            }

            TempData["username"] = username;
            return RedirectToAction("Verify");
        }

        [AllowAnonymous]
        public ActionResult SignUp()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("MyProfile");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult SignUp(User_Accounts ua, HttpPostedFileBase studentId, string ConfirmPass)
        {
           if (!ua.password.Equals(ConfirmPass))
            {
                ModelState.AddModelError(string.Empty, "Password does not match");
                return View(ua);
            }
            
            if (_userManager.SignUp(ua, ref ErrorMessage) != ErrorCode.Success)
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
                return View(ua);
            }
            
            var user = _userManager.GetUserByEmail(ua.email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User account creation failed.");
                return View(ua);
            }
            
            if (studentId != null && studentId.ContentLength > 0)
            {
                try
                {
                    string fileName = Path.GetFileName(studentId.FileName);
                    string filePath = Path.Combine(Server.MapPath("~/Uploads/StudentID/"), fileName);
                    
                    if (!Directory.Exists(Server.MapPath("~/Uploads/StudentID/")))
                    {
                        Directory.CreateDirectory(Server.MapPath("~/Uploads/StudentID/"));
                    }

                    studentId.SaveAs(filePath);
                    
                    var fileDocument = new File_Documents
                    {
                        user_id = user.id, 
                        file_document = "/Uploads/StudentID/" + fileName 
                    };
                    
                    _imageManager.CreateFileDocument(fileDocument, ref ErrorMessage);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Error saving Student ID: " + ex.Message);
                    return View(ua);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Student ID is required.");
                return View(ua);
            }
            TempData["SuccessMessage"] = "Registration successful. Admin will validate your registration.";
            return RedirectToAction("Verify", "Home");
        }

        [AllowAnonymous]
        public ActionResult SignUpForProvider()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("MyProfile");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult SignUpForProvider(User_Accounts ua, Store store, HttpPostedFileBase businessPermit, string ConfirmPass)
        {
            if (!ua.password.Equals(ConfirmPass))
            {
                ModelState.AddModelError(string.Empty, "Password does not match");
                return View(ua);
            }

            string errorMessage = string.Empty;

            if (_storeManager.CreateAccountAndStore(ua, store, ref errorMessage) != ErrorCode.Success)
            {
                ModelState.AddModelError(string.Empty, errorMessage);
                return View(ua);
            }

            if (businessPermit != null && businessPermit.ContentLength > 0)
            {
                try
                {
                    string fileName = Path.GetFileName(businessPermit.FileName);
                    string filePath = Path.Combine(Server.MapPath("~/Uploads/BusinessPermits/"), fileName);

                    if (!Directory.Exists(Server.MapPath("~/Uploads/BusinessPermits/")))
                    {
                        Directory.CreateDirectory(Server.MapPath("~/Uploads/BusinessPermits/"));
                    }

                    businessPermit.SaveAs(filePath);

                    var fileDocument = new File_Documents
                    {
                        user_id = ua.id,
                        file_document = "/Uploads/BusinessPermits/" + fileName
                    };

                    _imageManager.CreateFileDocument(fileDocument, ref errorMessage);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Error saving business permit: " + ex.Message);
                    return View(ua);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Business permit is required.");
                return View(ua);
            }

            TempData["SuccessMessage"] = "Registration successful. Admin will validate your registration.";
            return RedirectToAction("Verify");
        }

        public bool CreateFileDocument(File_Documents fileDocument, ref string errorMessage)
        {
            try
            {
                using (var context = new UCGrabEntities())
                {
                    context.File_Documents.Add(fileDocument);
                    context.SaveChanges();
                }
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        [AllowAnonymous]
        public ActionResult SignUpForDriver()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("MyProfile");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult SignUpForDriver(User_Accounts ua, HttpPostedFileBase businessPermit, string ConfirmPass)
        {
            if (!ua.password.Equals(ConfirmPass))
            {
                ModelState.AddModelError(string.Empty, "Password does not match");
                return View(ua);
            }

            if (_userManager.SignUp(ua, ref ErrorMessage) != ErrorCode.Success)
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
                return View(ua);
            }

            var user = _userManager.GetUserByEmail(ua.email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User account creation failed.");
                return View(ua);
            }

            if (businessPermit != null && businessPermit.ContentLength > 0)
            {
                try
                {
                    string fileName = Path.GetFileName(businessPermit.FileName);
                    string filePath = Path.Combine(Server.MapPath("~/Uploads/BusinessPermits/"), fileName);

                    if (!Directory.Exists(Server.MapPath("~/Uploads/BusinessPermits/")))
                    {
                        Directory.CreateDirectory(Server.MapPath("~/Uploads/BusinessPermits/"));
                    }

                    businessPermit.SaveAs(filePath);

                    var fileDocument = new File_Documents
                    {
                        user_id = user.id,
                        file_document = "/Uploads/BusinessPermits/" + fileName
                    };

                    _imageManager.CreateFileDocument(fileDocument, ref ErrorMessage);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Error saving business permit: " + ex.Message);
                    return View(ua);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Business permit is required.");
                return View(ua);
            }
            
            TempData["SuccessMessage"] = "Registration successful. Admin will validate your registration.";
            return RedirectToAction("Verify");
        }

        [Authorize]
        public ActionResult MyProfile()
        {
            IsUserLoggedSession();
            var user = User.Identity.Name;
            var userinfo = _userManager.GetUserInfoByUsername(user);

            if(userinfo == null)
            {
                TempData["ErrorMessage"] = "Failed retreiving user information.";
                return RedirectToAction("MyProfile");

            }
            return View(userinfo);
        }
        [HttpPost]
        public ActionResult MyProfile(User_Information userInf, HttpPostedFileBase profilePicture)
        {
            IsUserLoggedSession();

            if (ModelState.IsValid)
            {
                var user = _userManager.GetUserByUserId(userInf.user_id);
                if(user == null)
                {
                    TempData["ErrorMessage"] = "User Not Found,";
                    return View(userInf);
                }
            }
            if (profilePicture != null && profilePicture.ContentLength > 0)
            {
                var uploadsFolderPath = Server.MapPath("~/UploadedFiles/");
                if (!Directory.Exists(uploadsFolderPath))
                    Directory.CreateDirectory(uploadsFolderPath);

                var profileFileName = Path.GetFileName(profilePicture.FileName);
                var profileSavePath = Path.Combine(uploadsFolderPath, profileFileName);
                profilePicture.SaveAs(profileSavePath);

                var existingImage = _imageManager.ListImgAttachByImageId(userInf.id).FirstOrDefault();
                if (existingImage != null)
                {
                    existingImage.image_file = profileFileName;
                    if (_imageManager.UpdateImg(existingImage, ref ErrorMessage) == ErrorCode.Error)
                    {
                        ModelState.AddModelError(String.Empty, ErrorMessage);
                        return View(userInf);
                    }
                }
                
            }
            return View(userInf);
        }
        [Authorize]
        public ActionResult EditProfile()
        {
            IsUserLoggedSession();

            var user = User.Identity.Name;
            var usrinfo = _userManager.GetUserInfoByUsername(user);

            if (usrinfo == null)
            {
                TempData["ErrorMessage"] = "Failed retrieving user information.";
                return RedirectToAction("MyProfile");
            }

            return View(usrinfo);
        }

        [HttpPost]
        public ActionResult EditProfile(User_Information userInf, HttpPostedFileBase profilePicture)
        {
            IsUserLoggedSession();

            if (ModelState.IsValid)
            {
                var user = _userManager.GetUserByUserId(userInf.user_id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Error updating profile: User not found.";
                    return View(userInf);
                }

                if (profilePicture != null && profilePicture.ContentLength > 0)
                {
                    var uploadsFolderPath = Server.MapPath("~/UploadedFiles/");
                    if (!Directory.Exists(uploadsFolderPath))
                        Directory.CreateDirectory(uploadsFolderPath);

                    var profileFileName = Path.GetFileName(profilePicture.FileName);
                    var profileSavePath = Path.Combine(uploadsFolderPath, profileFileName);
                    profilePicture.SaveAs(profileSavePath);

                    System.Diagnostics.Debug.WriteLine("Profile picture saved at: " + profileSavePath);

                    var existingImage = _imageManager.ListImgAttachByImageId(userInf.id).FirstOrDefault();
                    if (existingImage != null)
                    {
                        var oldImagePath = Path.Combine(uploadsFolderPath, existingImage.image_file);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }

                        existingImage.image_file = profileFileName;
                        if (_imageManager.UpdateImg(existingImage, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(string.Empty, ErrorMessage);
                            return View(userInf);
                        }
                    }
                    else
                    {
                        DbImage dbImage = new DbImage
                        {
                            image_file = profileFileName,
                            image_id = userInf.id
                        };

                        if (_imageManager.CreateImg(dbImage, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(string.Empty, ErrorMessage);
                            return View(userInf);
                        }
                    }
                }

                if (_userManager.UpdateUserInformation(userInf, ref ErrorMessage) == ErrorCode.Error)
                {
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                    return View(userInf);
                }

                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction("MyProfile");
            }

            return View(userInf);
        }



        [AllowAnonymous]
        public ActionResult ForgotPass()
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
        public ActionResult Shop(String storeId, int? categoryId)
        {
            var store = _storeManager.GetStoreByUserId(storeId);

            if (store != null)
            {
                var products = categoryId.HasValue
                    ? _productManager.ListActiveProductByCategory(store.user_id, categoryId.Value)
                    : _productManager.ListActiveProduct(store.user_id);

                ViewBag.SelectedCategory = categoryId;
                ViewBag.StoreName = store.store_name;
                return View(products);
            }

            return HttpNotFound("Store not found");
        }

        [AllowAnonymous]
        public ActionResult ShopList(int? categoryId)
        {
            var stores = categoryId.HasValue
                ? _storeManager.ListStoresByCategory(categoryId.Value)
                : _storeManager.ListStore();

            ViewBag.SelectedCategory = categoryId; 
            ViewBag.CategoryName = categoryId.HasValue
                ? ((Categories)categoryId.Value).ToString()
                : "All Categories";

            return View(stores);
        }

        [AllowAnonymous]
        public ActionResult ShopAll()
        {
            var products = _productManager.ListAll();
            return View(products);
 
        }

        [AllowAnonymous]
        public ActionResult Detail(int? id)
        {
            if (id == null || id == 0)
                return RedirectToAction("PageNotFound");

            var product = _productManager.GetProductById(id);

            return View(product);

        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult AddCart(int prodId, int qty, decimal price)
        {
            var response = new Response();

            try
            {
                var result = _orderManager.AddCart(UserId, prodId, qty, price, ref ErrorMessage);

                if (result == ErrorCode.Error)
                {
                    throw new Exception(ErrorMessage);
                }

                response.code = (int)ErrorCode.Success;
                response.message = "Item added to the cart!";
            }
            catch (Exception ex)
            {
                response.code = (int)ErrorCode.Error;
                response.message = $"An error occurred while adding the item to the cart: {ex.Message}";

                if (ex.InnerException != null)
                {
                    response.message += $" Inner exception: {ex.InnerException.Message}";
                }

                LogError(ex);
            }

            return Json(response, JsonRequestBehavior.AllowGet);
        }

        private void LogError(Exception ex)
        {
            string logDirectory = @"C:\Logs";  // Use a local directory

            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            string logFilePath = Path.Combine(logDirectory, "errorlog.txt");
            string errorMessage = DateTime.Now.ToString() + " - " + ex.ToString();

            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine(errorMessage);
            }

        }
        [AllowAnonymous]
        public ActionResult Cart()
        {
            var orders = _orderManager.GetOrderByUserId(UserId);
            var groupedOrders = orders
                .GroupBy(order => order.Store?.store_name)
                .ToList();

            var _db = new UCGrabEntities();
            var availableVouchers = _db.Vouchers.Where(v =>
                v.is_active == 1 &&
                (v.start_date == null || v.start_date <= DateTime.Now) &&
                (v.end_date == null || v.end_date >= DateTime.Now)).ToList();

            ViewBag.AvailableVouchers = availableVouchers;

            return View(groupedOrders);
        }
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Cart(int? qty, int orderDtId, string action)
        {
            switch (action)
            {
                case "X":
                    _orderManager.DeleteOrderDetail(orderDtId, ref ErrorMessage);
                    break;
                default:
                    if (qty <= 0)
                    {
                        _orderManager.DeleteOrderDetail(orderDtId, ref ErrorMessage);
                    }
                    else
                    {
                        var orderDt = _orderManager.GetOrderDetailById(orderDtId);
                        orderDt.quatity = qty;
                        _orderManager.UpdateOrderDetail(orderDt.id, orderDt, ref ErrorMessage);
                    }
                    break;
            }

            var updatedOrders = _orderManager.GetOrderByUserId(UserId)
                .Where(order => order.Order_Detail.Any()) 
                .ToList();

            var groupedOrders = updatedOrders
                .GroupBy(order => order.Store?.store_name)
                .Where(group => group.Any())
                .ToList();

            var _db = new UCGrabEntities();
            var availableVouchers = _db.Vouchers.Where(v =>
                v.is_active == 1 &&
                (v.start_date == null || v.start_date <= DateTime.Now) &&
                (v.end_date == null || v.end_date >= DateTime.Now)).ToList();

            ViewBag.AvailableVouchers = availableVouchers;

            return View(groupedOrders);
        }


        public JsonResult GetCartCount()
        {
            int count = _orderManager.GetCartCountByUserId(UserId);
            var res = new { count };
            return Json(res, JsonRequestBehavior.AllowGet);
        }



        [AllowAnonymous]
        public ActionResult FavoritedProduct()
        {
            var fav = _favManager.GetAllProduct(UserId);
            return View(fav);
        }
        public JsonResult GetFavoriteCount()
        {
            var res = new { count = _orderManager.GetFavoriteCountByUserId(UserId) };

            return Json(res, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [AllowAnonymous]
        public JsonResult AddToFavorites(int prodId, int qty)
        {
            var res = new Response();

            try
            {
                if (_orderManager.AddToFavorites(UserId, prodId, qty, ref ErrorMessage) == ErrorCode.Error)
                {
                    throw new Exception(ErrorMessage);
                }

                res.code = (int)ErrorCode.Success;
                res.message = "Item Added!";
            }
            catch (Exception ex)
            {
                res.code = (int)ErrorCode.Error;
                res.message = "" + ex.Message;

                // Log the inner exception if it exists
                if (ex.InnerException != null)
                {
                    res.message += " Inner exception: " + ex.InnerException.Message;
                }

                LogError(ex);
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        [AllowAnonymous]
        public JsonResult RemoveFromFavorites(int prodId)
        {
            var res = new Response();

            try
            {
                if (_favManager.RemoveFromFavorites(UserId, prodId, ref ErrorMessage) == ErrorCode.Error)
                {
                    res.code = (int)ErrorCode.Success;
                    res.message = "Item removed from favorites!";
                }

                throw new Exception(ErrorMessage);
            }
            catch (Exception ex)
            {
                res.code = (int)ErrorCode.Error;
                res.message = "" + ex.Message;
                LogError(ex);
            }

            return Json(res, JsonRequestBehavior.AllowGet);
        }
        [AllowAnonymous]
        public ActionResult CheckOut()
        {
            IsUserLoggedSession();

            var userId = UserId;
            var userInfo = _userManager.GetUserInfoByUserId(userId);
            var order = _orderManager.GetOpenOrderByUserId(userId);

            if (order == null)
            {
                ViewBag.Error = "No open order found. Please add products to your cart.";
                return RedirectToAction("Cart");
            }

            var orderDetails = _orderManager.GetOrderDetailsByOrderId(order.order_id) ?? new List<Order_Detail>();
            var totalOrder = orderDetails.Sum(od => od.price * od.quatity);
            var storeQrCode = _imageManager.GetStoreQrCodeByStoreId(order.store_id);

            var model = new CheckOutViewModel
            {
                OrderId = order.order_id,
                Firstname = userInfo?.first_name ?? "N/A",
                Lastname = userInfo?.last_name ?? "N/A",
                Phone = userInfo?.phone ?? "N/A",
                Email = userInfo?.email ?? "N/A",
                Products = orderDetails.Select(od => new ProductViewModel
                {
                    ProductName = od?.Product?.product_name ?? "Unknown Product",
                    Quantity = (int)(od?.quatity ?? 0),
                    Price = (int)(od?.price ?? 0)
                }).ToList(),
                CheckOutOption = (int)CheckoutOption.PickUp,
                PaymentMethod = (int)PayMethod.GCash,
                Total = totalOrder,
                StoreQrCode = storeQrCode
            };

            return View(model);
        }


        [HttpPost]
        [AllowAnonymous]
        public ActionResult Checkout(CheckOutViewModel model, HttpPostedFileBase gcashReceipt)
        {
            IsUserLoggedSession();

            try
            {
                string errorMessage = string.Empty;
                string filePath = string.Empty;

                // Upload the receipt if provided
                if (gcashReceipt != null && gcashReceipt.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(gcashReceipt.FileName);
                    string directoryPath = Server.MapPath("~/Uploads/Receipts/");

                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    filePath = Path.Combine(directoryPath, fileName);
                    gcashReceipt.SaveAs(filePath);
                    filePath = "/Uploads/Receipts/" + fileName;
                }
                else
                {
                    ViewBag.Error = "Please upload a receipt.";
                    return View(model);
                }

                // Place the order
                var result = _orderManager.PlaceOrder(UserId, model, filePath, null, ref errorMessage);

                if (result == ErrorCode.Success)
                {
                    var order = _orderManager.GetLastOrderByUserId(UserId);
                    if (order == null)
                    {
                        ViewBag.Error = "Order not found.";
                        return View(model);
                    }

                    model.StoreId = (int)order.store_id;
                    model.StoreName = _storeManager.GetStoreById(order.store_id)?.store_name ?? "Unknown Store";

                    model.Products = order.Order_Detail.Select(od => new ProductViewModel
                    {
                        ProductName = od.Product.product_name,
                        Quantity = (int)od.quatity,
                        Price = (decimal)od.price
                    }).ToList();

                    model.Total = model.Products.Sum(p => p.Price * p.Quantity);

                    var productManager = new ProductManager();  
                    foreach (var product in model.Products)
                    {
                        int productId = product.ProductId;
                        int orderedQuantity = product.Quantity;

                        productManager.UpdateStock(productId, orderedQuantity);
                    }

                    var invoiceFilePath = GenerateInvoicePDF(model);
                    _orderManager.UpdateOrderInvoicePath(order.order_id, invoiceFilePath);

                    return RedirectToAction("MyOrders");
                }
                else
                {
                    ViewBag.Error = errorMessage;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        private string GenerateInvoicePDF(CheckOutViewModel model)
        {
            string invoiceDirectory = Server.MapPath("~/Invoices/");

            if (!Directory.Exists(invoiceDirectory))
            {
                Directory.CreateDirectory(invoiceDirectory);
            }

            string fileName = $"Invoice_{DateTime.Now.Ticks}.pdf";
            string filePath = Path.Combine(invoiceDirectory, fileName);

            var pageSize = new iTextSharp.text.Rectangle(
                Utilities.InchesToPoints(4.5f),
                Utilities.InchesToPoints(8.5f)
            );

            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                using (Document doc = new Document(pageSize, 36, 36, 20, 20)) // Add margins
                {
                    PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                    doc.Open();

                    PdfContentByte canvas = writer.DirectContent;

                    float contentWidth = pageSize.Width - doc.LeftMargin - doc.RightMargin;

                    var ucGrabLogoPath = Server.MapPath("~/Assets/Shop/img/5-removebg-preview.png");
                    iTextSharp.text.Image ucGrabLogo = iTextSharp.text.Image.GetInstance(ucGrabLogoPath);
                    ucGrabLogo.ScaleAbsolute(100, 100); 

                    PdfPTable logoTable = new PdfPTable(1); 
                    logoTable.WidthPercentage = 100;

                    PdfPCell logoCell = new PdfPCell(ucGrabLogo)
                    {
                        Border = Rectangle.NO_BORDER, 
                        HorizontalAlignment = Element.ALIGN_CENTER, 
                        VerticalAlignment = Element.ALIGN_MIDDLE 
                    };

                    logoTable.AddCell(logoCell); 
                    doc.Add(logoTable);

                    doc.Add(new Paragraph("\n\n"));
                    var headerText = new StringBuilder()
                        .AppendLine("University of Cebu Lapu-Lapu Mandaue")
                        .AppendLine("A.C. Cortes Ave., Looc Mandaue City")
                        .AppendLine("")

                        .AppendLine("-----------------------------------------------------------")
                        .AppendLine("Sales INVOICE - CUSTOMER COPY")
                        .AppendLine("-----------------------------------------------------------");

                    ColumnText headerColumn = new ColumnText(canvas);
                    headerColumn.SetSimpleColumn(
                        doc.LeftMargin,
                        pageSize.Height - doc.TopMargin - 170, // Adjust to give more space for the header
                        doc.LeftMargin + contentWidth,
                        pageSize.Height - doc.TopMargin - 80
                    );
                    headerColumn.AddText(new Phrase(headerText.ToString(), new Font(Font.FontFamily.HELVETICA, 12, Font.BOLD)));
                    headerColumn.Alignment = Element.ALIGN_CENTER;
                    headerColumn.Go();

                    float currentY = headerColumn.YLine - 20;

                    var bodyText = new StringBuilder();
                    bodyText.AppendLine(model.StoreName?.Trim() ?? string.Empty)
                            .AppendLine("==========================================")
                            .AppendLine($"Date: {DateTime.Now:yyyy-MM-dd}                                  Time: {DateTime.Now:HH:mm:ss}")
                            .AppendLine("==========================================");

                    if (model.Products != null && model.Products.Any())
                    {
                        bodyText.AppendLine("Products                               Qty                         Price");
                        foreach (var product in model.Products)
                        {
                            bodyText.AppendLine(
                                $"{product.ProductName?.Trim(),-25}           {product.Quantity,-10}             ₱{product.Price,10:N2}"
                            );
                        }
                    }
                    else
                    {
                        bodyText.AppendLine("No products found in this order.");
                    }

                    var totalOrder = model.Products?.Sum(p => p.Price * p.Quantity) ?? 0;
                    bodyText.AppendLine("------------------------------------------------------------------------")
                            .AppendLine($"Total: ₱{totalOrder:N2}".PadLeft(80))
                            .AppendLine("------------------------------------------------------------------------")
                            .AppendLine("")
                            .AppendLine($"Order ID: {model.OrderNumber}")
                            .AppendLine($"Customer Name: {model.Lastname?.Trim()}, {model.Firstname?.Trim()}")
                            .AppendLine($"Contact: {model.Phone?.Trim()}")
                            .AppendLine($"Order Date: {DateTime.Now:yyyy-MM-dd}")
                            .AppendLine("------------------------------------------------------------------------");

                    ColumnText bodyColumn = new ColumnText(canvas);
                    bodyColumn.SetSimpleColumn(
                        doc.LeftMargin,
                        doc.BottomMargin - 50, // Leave space above the footer
                        doc.LeftMargin + contentWidth,
                        currentY
                    );
                    bodyColumn.AddText(new Phrase(bodyText.ToString(), new Font(Font.FontFamily.HELVETICA, 10)));
                    bodyColumn.Alignment = Element.ALIGN_LEFT;
                    bodyColumn.Go();

                    // Footer Section
                    var footerText = new StringBuilder()
                        .AppendLine("Tell us about your experience.")
                        .AppendLine("Contact Us: ucgrab@gmail.com")
                        .AppendLine("Visit us also at ucgrablm.somee.com")
                        .AppendLine("------------------------------------------------------")
                        .AppendLine("This serves as a SALES INVOICE");

                    ColumnText footerColumn = new ColumnText(canvas);
                    footerColumn.SetSimpleColumn(
                        doc.LeftMargin,
                        doc.BottomMargin, // Ensure it starts from the bottom margin
                        doc.LeftMargin + contentWidth,
                        doc.BottomMargin + 80 // Define a height for the footer
                    );
                    footerColumn.AddText(new Phrase(footerText.ToString(), new Font(Font.FontFamily.HELVETICA, 10, Font.ITALIC)));
                    footerColumn.Alignment = Element.ALIGN_CENTER;
                    footerColumn.Go();

                    doc.Close();
                }
            }

            return "/Invoices/" + fileName;
        }


        public static class Utilities
        {
            public static float InchesToPoints(float inches)
            {
                return inches * 72; // 1 inch = 72 points
            }
        }



        [AllowAnonymous]
        public ActionResult Contact()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Contact(ContactUs inquiry)
        {
            if (ModelState.IsValid)
            {
                using (var db = new UCGrabEntities())
                {
                    var userId = UserId;
                    inquiry.user_id = userId;

                    db.ContactUs.Add(inquiry);
                    db.SaveChanges();
                }

                TempData["Message"] = "Your inquiry has been submitted successfully!";
                return RedirectToAction("Contact");
            }

            return View(inquiry);
        }
        [AllowAnonymous]
        public ActionResult MyOrders()
        {
            IsUserLoggedSession();

            var _db = new UCGrabEntities();
            var userId = UserId; 
            var orders = _orderManager.GetUserOrderByUserId(userId);

            ViewBag.ToReceiveCount = orders.Count(o => o.order_status == 1 || o.order_status == 3 || o.order_status == 4);
            ViewBag.ToReviewCount = orders.Count(o => o.order_status == 5);
            ViewBag.CancelledCount = orders.Count(o => o.order_status == 2);
            ViewBag.RejectedCount = orders.Count(o => o.order_status == 7);


            var model = orders.Select(order => new OrderViewModel
            {
                OrderId = order.order_id,
                OrderNumber = order.order_id.ToString(),
                OrderDate = order.order_date.HasValue ? order.order_date.Value : DateTime.MinValue,
                Status = ((OrderStatus)order.order_status).ToString(),
                Products = order.Order_Detail.Select(od => new ProductViewModel
                {
                    ProductName = od.Product.product_name,
                    Quantity = (Int32)od.quatity,
                    Price = (Int32)od.price,
                    ImageFilePath = od.Product.Image_Product.FirstOrDefault().image_file
                }).ToList(),
                Total = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity),
                InvoicePath = order.invoice 

            }).ToList();

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CancelOrder(int orderId)
        {
            IsUserLoggedSession();

            try
            {
                var result = _orderManager.CancelOrder(orderId, UserId);
                if (result == ErrorCode.Success)
                {
                    return RedirectToAction("MyOrders");
                }
                else
                {
                    // Handle error
                    ViewBag.Error = "Failed to cancel the order.";
                    return RedirectToAction("MyOrders");
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                ViewBag.Error = ex.Message;
                return RedirectToAction("MyOrders");
            }
        }

        [AllowAnonymous]
        public ActionResult PageNotFound()
        {
            return View();
        }

        [HttpPost]
        public ActionResult SubmitReview(Review review, int orderId)
        {
            var _db = new UCGrabEntities();

            if (review == null || review.rating == 0 || string.IsNullOrEmpty(review.comment))
            {
                TempData["ErrorMessage"] = "Invalid review data.";
                return RedirectToAction("MyOrders");
            }

            var username = User.Identity.Name;
            var userInfo = _userManager.GetUserInfoByUsername(username);

            if (userInfo == null)
            {
                TempData["ErrorMessage"] = "User is not logged in.";
                return RedirectToAction("MyOrders");
            }

            int userId = userInfo.id;

            var reviewEntity = new Review
            {
                order_id = orderId,
                user_id = userId,
                rating = review.rating,
                comment = review.comment,
                review_date = DateTime.Now
            };

            _db.Review.Add(reviewEntity);
            _db.SaveChanges();

            TempData["SuccessMessage"] = "Review submitted successfully.";
            return RedirectToAction("MyOrders");
        }

        private IActionResult Unauthorized(string v)
        {
            throw new NotImplementedException();
        }

        private IActionResult BadRequest(string v)
        {
            throw new NotImplementedException();
        }

        public ActionResult DiscountedProducts()
        {
            var db = new UCGrabEntities();

            var allDiscountedProducts = (from p in db.Product
                                         join d in db.Discounts on p.store_id equals d.store_id into discountGroup
                                         from d in discountGroup.DefaultIfEmpty()
                                         let hasProductDiscount = d != null && d.product_id == p.id && d.is_active == 1 && d.start_date <= DateTime.Now && d.end_date >= DateTime.Now
                                         let hasStoreDiscount = d != null && d.product_id == null && d.is_active == 1 && d.start_date <= DateTime.Now && d.end_date >= DateTime.Now
                                         let discountedPrice = hasProductDiscount
                                                               ? p.price - (p.price * (d.discount_value ?? 0) / 100)  
                                                               : hasStoreDiscount
                                                               ? p.price - (p.price * (d.discount_value ?? 0) / 100)  
                                                               : p.price  
                                         where hasProductDiscount || hasStoreDiscount  
                                         select new ProductViewModel
                                         {
                                             ProductId = p.id,
                                             ProductName = p.product_name,
                                             OriginalPrice = p.price,
                                             Price = discountedPrice, 
                                             ImageFilePath = db.Image_Product.FirstOrDefault(i => i.product_id == p.id).image_file,
                                             Description = p.product_description,
                                             IsDiscounted = true  
                                         }).Distinct().ToList(); 

            return View(allDiscountedProducts);
        }
        [HttpPost]
        public ActionResult ApplyCoupon(string couponCode)
        {
            var _db = new UCGrabEntities();

            if (string.IsNullOrEmpty(couponCode))
            {
                TempData["Error"] = "Please enter a coupon code.";
                return RedirectToAction("Cart");
            }

            var voucher = _db.Vouchers.FirstOrDefault(v => v.voucher_code == couponCode && v.is_active == 1);

            if (voucher == null || (voucher.start_date != null && voucher.start_date > DateTime.Now) || (voucher.end_date != null && voucher.end_date < DateTime.Now))
            {
                TempData["Error"] = "Invalid or expired coupon code.";
                return RedirectToAction("Cart");
            }

            var userId = UserId; 

            var hasUsedVoucher = _db.VoucherUsage.Any(vu => vu.user_id == userId && vu.voucher_id == voucher.voucher_id);

            if (hasUsedVoucher)
            {
                TempData["Error"] = "You have already used this voucher.";
                return RedirectToAction("Cart");
            }

            if (voucher.max_uses.HasValue && voucher.max_uses <= 0)
            {
                TempData["Error"] = "This voucher has already been fully redeemed.";
                return RedirectToAction("Cart");
            }

            var orders = _db.Order.Where(o => o.user_id == userId && o.order_status == 0 && o.store_id == voucher.store_id).ToList();

            decimal cartSubtotal = (decimal)orders.Sum(order => order.Order_Detail.Sum(od => od.quatity * od.Product.price));

            if (cartSubtotal < voucher.min_order_amount)
            {
                TempData["Error"] = "Your cart total does not meet the minimum order amount for this voucher.";
                return RedirectToAction("Cart");
            }

            decimal voucherAmount = 0;

            foreach (var order in orders)
            {
                foreach (var orderDetail in order.Order_Detail)
                {
                    decimal originalPrice = (decimal)(orderDetail.price ?? 0);
                    decimal discountValue = voucher.discount_type == "Percentage"
                        ? originalPrice * (voucher.discount_value ?? 0) / 100
                        : (voucher.discount_value ?? 0);

                    orderDetail.price = originalPrice - discountValue;
                    voucherAmount += discountValue;
                }
            }

            if (voucher.max_uses.HasValue)
            {
                voucher.max_uses -= 1;

                if (voucher.max_uses <= 0)
                {
                    voucher.is_active = 0;
                }
            }

            _db.VoucherUsage.Add(new VoucherUsage
            {
                user_id = userId,
                voucher_id = voucher.voucher_id,
                usage_date = DateTime.Now
            });

            _db.SaveChanges();

            ViewBag.VoucherAmount = voucherAmount;

            TempData["Success"] = "Coupon applied successfully!";
            return RedirectToAction("Cart");
        }

    }
}
