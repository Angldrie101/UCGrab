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
using UCGrab.Repository;

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
                    return RedirectToAction("ChangePass");
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
        public ActionResult ChangePass()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public ActionResult ChangePass(string username, string email, string password)
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

        [AllowAnonymous]
        public ActionResult Verify()
        {
            if (String.IsNullOrEmpty(TempData["username"] as String))
                return RedirectToAction("Login");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Verify(string verify_code, string username)
        {
            if (String.IsNullOrEmpty(username))
                return RedirectToAction("Login");

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
            var store = _storeManager.CreateOrRetrieve(username, ref ErrorMessage);
            store.user_id = user.user_id; 
            _storeManager.UpdateStore(store.id,store, ref ErrorMessage);


            SendActivationNotificationEmail(user.email);
            
            return RedirectToAction("Login");
        }

        private void SendActivationNotificationEmail(string userEmail)
        {
            string emailBody = "Your account has been activated successfully.";
            string errorMessage = "";

            var mailManager = new MailManager();
            bool emailSent = mailManager.SendEmail(userEmail, "Account Activation Notification", emailBody, ref errorMessage);

            if (!emailSent)
            {
                // Handle email sending failure
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
                return RedirectToAction("Index");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult SignUp(User_Accounts ua, string ConfirmPass)
        {
            if (!ua.password.Equals(ConfirmPass))
            {
                ModelState.AddModelError(String.Empty, "Password does not match");
                return View(ua);
            }

            if (_userManager.SignUp(ua, ref ErrorMessage) != ErrorCode.Success)
            {
                ModelState.AddModelError(String.Empty, ErrorMessage);
                return View(ua);
            }

            var user = _userManager.GetUserByEmail(ua.email);
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

            TempData["username"] = ua.username;
            return RedirectToAction("Verify");
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
        public ActionResult SignUpForProvider(User_Accounts ua, HttpPostedFileBase businessPermit, string ConfirmPass)
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
            return RedirectToAction("Login", "Home");
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
            return RedirectToAction("Login", "Home");

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

                // Handle profile picture upload
                if (profilePicture != null && profilePicture.ContentLength > 0)
                {
                    var uploadsFolderPath = Server.MapPath("~/UploadedFiles/");
                    if (!Directory.Exists(uploadsFolderPath))
                        Directory.CreateDirectory(uploadsFolderPath);

                    var profileFileName = Path.GetFileName(profilePicture.FileName);
                    var profileSavePath = Path.Combine(uploadsFolderPath, profileFileName);
                    profilePicture.SaveAs(profileSavePath);

                    // Log the save path
                    System.Diagnostics.Debug.WriteLine("Profile picture saved at: " + profileSavePath);

                    var existingImage = _imageManager.ListImgAttachByImageId(userInf.id).FirstOrDefault();
                    if (existingImage != null)
                    {
                        existingImage.image_file = profileFileName;
                        if(_imageManager.UpdateImg(existingImage, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            return View(userInf);
                        }
                    }
                    else
                    {
                        Image img = new Image
                        {
                            image_file = profileFileName,
                            image_id = userInf.id
                        };

                       if (_imageManager.CreateImg(img, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            return View(userInf);
                        }
                    }
                }

           if (_userManager.UpdateUserInformation(userInf, ref ErrorMessage) == ErrorCode.Error)
                {
                    ModelState.AddModelError(String.Empty, ErrorMessage);
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
        public ActionResult Shop(String storeId)
        {
            var store = _storeManager.GetStoreByUserId(storeId);

            if (store != null)
            {
                var products = _productManager.ListActiveProduct(store.user_id) ?? new List<Product>();
                return View(products);
            }

            // Optionally, handle the case where the store is not found
            return HttpNotFound("Store not found");
        }

        [AllowAnonymous]
        public ActionResult ShopList()
        {
            var stores = _storeManager.ListStore();

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
        public JsonResult AddCart(int prodId, int qty)
        {
            var res = new Response();

            try
            {
                if (_orderManager.AddCart(UserId, prodId, qty, ref ErrorMessage) == ErrorCode.Error)
                {
                    throw new Exception(ErrorMessage);
                }

                res.code = (int)ErrorCode.Success;
                res.message = "Item Added!";
            }
            catch (Exception ex)
            {
                res.code = (int)ErrorCode.Error;
                res.message = "An error occurred while adding the item to the cart: " + ex.Message;

                // Log the inner exception if it exists
                if (ex.InnerException != null)
                {
                    res.message += " Inner exception: " + ex.InnerException.Message;
                }

                LogError(ex);
            }

            return Json(res, JsonRequestBehavior.AllowGet);

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
            return View(groupedOrders);
        }
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Cart(int qty, int orderDtId, String action)
        {
            switch (action)
            {
                case "&plus;":
                    qty++;
                    break;
                case "&minus;":
                    qty--;
                    break;
                case "X":
                    _orderManager.DeleteOrderDetail(orderDtId, ref ErrorMessage);
                    return View(_orderManager.GetOrderByUserId(UserId));
            }

            if (qty <= 0)
            {
                _orderManager.DeleteOrderDetail(orderDtId, ref ErrorMessage);
                return View(_orderManager.GetOrderByUserId(UserId));
            }

            var orderDt = _orderManager.GetOrderDetailById(orderDtId);
            orderDt.quatity = qty;

            _orderManager.UpdateOrderDetail(orderDt.id, orderDt, ref ErrorMessage);

            return View(_orderManager.GetOrderByUserId(UserId));
        }

        public JsonResult GetCartCount()
        {
            var res = new { count = _orderManager.GetCartCountByUserId(UserId) };

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        [AllowAnonymous]
        public ActionResult FavoritedProduct()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult CheckOut()
        {
            IsUserLoggedSession();

            var userId = UserId;
            var userInfo = _userManager.GetUserInfoByUserId(userId);
            var order = _orderManager.GetOrderByUserId(userId).FirstOrDefault();
            var orderDetails = _orderManager.GetOrderDetailsByOrderId(order.order_id);

            var model = new OrderViewModel
            {
                OrderId = order.order_id,
                Firstname = userInfo.first_name,
                Lastname = userInfo.last_name,
                Phone = userInfo.phone,
                Email = userInfo.email,
                Products = orderDetails.Select(od => new ProductViewModel
                {
                    ProductName = od.Product.product_name,
                    Quantity = (Int32)od.quatity,
                    Price = (Int32)od.price
                }).ToList(),
                Total = orderDetails.Sum(od => (od.price ?? 0) * (od.quatity ?? 0)), // Handling nulls
                CheckOutOption = (Int32)CheckoutOption.PickUp,
                PaymentMethod = (Int32)PayMethod.GCash
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult CheckOut(OrderViewModel model, string gcashReceipt)
        {
            IsUserLoggedSession();

            try
            {
                string errorMessage = string.Empty;
                var result = _orderManager.PlaceOrder(UserId, model, gcashReceipt, ref errorMessage);

                if (result == ErrorCode.Success)
                {
                    return RedirectToAction("MyOrders");
                }
                else
                {
                    // Handle error
                    ViewBag.Error = errorMessage;
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                // Handle exception
                ViewBag.Error = ex.Message;
                return View(model);
            }
        }

        [AllowAnonymous]
        public ActionResult Contact()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult MyOrders()
        {
            IsUserLoggedSession();

            var userId = UserId; // Assuming you have a way to get the logged-in user's ID
            var orders = _orderManager.GetUserOrderByUserId(userId);

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
                Total = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity)
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
    }
}