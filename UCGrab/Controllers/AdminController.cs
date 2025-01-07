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

            var recentStores = _db.Store
                .OrderByDescending(s => s.store_id) // Assuming 'CreatedAt' column exists
                .Take(5)
                .ToList();

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

        [HttpPost]
        [AllowAnonymous]
        public ActionResult UploadUserAccounts()
        {
            var db = new UCGrabEntities();
            var file = Request.Files["fileUpload"]; // Get the uploaded file

            if (file != null && file.ContentLength > 0)
            {
                // Set the path where the file will be saved on the server
                string folderPath = Server.MapPath("~/Uploads");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Set the file path (use the original file name)
                string filePath = Path.Combine(folderPath, file.FileName);

                // Save the file to the server
                file.SaveAs(filePath);

                // Process the file (e.g., read the file to import user accounts)
                try
                {
                    using (var reader = new StreamReader(file.InputStream))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            // Split the line by commas assuming it's a CSV
                            string[] fields = line.Split(',');

                            if (fields.Length >= 4) // Ensure enough columns are available
                            {
                                string userid = fields[0].Trim();
                                string username = fields[1].Trim();
                                string password = fields[2].Trim();
                                string roleId = fields[3].Trim();
                                string status = fields[4].Trim();
                                string verify_code = fields[5].Trim();
                                string date_created = fields[6].TrimEnd();

                                // Insert the new user account into the database
                                // Assuming you have a method to add user account
                                AddUserAccount(userid, username, password, roleId, status, verify_code,date_created);
                            }
                        }
                    }

                    TempData["SuccessMessage"] = "File uploaded and accounts added successfully.";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"An error occurred while processing the file: {ex.Message}";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "No file uploaded or file is empty.";
            }

            return RedirectToAction("UserAccounts");
        }
        private void AddUserAccount(string userid, string username, string password, string roleId, string status, string verify_code,string date_created)
        {
            DateTime? parsedDate = null;

            try
            {
                parsedDate = DateTime.Parse(date_created);
            }
            catch (FormatException)
            {
                // Handle invalid date format if needed, or leave parsedDate as null
                // Optionally log the error or handle differently
            }
            // Example code to save to the database, modify according to your context
            var userAccount = new User_Accounts
            {
                user_id = userid,
                username = username,
                password = password, // You might want to hash the password
                role_id = int.Parse(roleId),
                status = int.Parse(status),
                verify_code = verify_code,
                date_created = parsedDate
            };

            // Assuming you have a database context
            using (var context = new UCGrabEntities())
            {
                context.User_Accounts.Add(userAccount);
                context.SaveChanges();
            }
        }
        [HttpPost]
        public JsonResult UserDelete(int id)
        {
            var res = new Response();
            string errorMessage = string.Empty;

            try
            {
                res.code = (int)_userManager.DeleteUser(id, ref errorMessage);
                res.message = res.code == 1 ? "User deleted successfully." : errorMessage;
            }
            catch (Exception ex)
            {
                res.code = 0;
                res.message = $"An error occurred: {ex.Message}";
            }

            return Json(res);
        }
        [HttpPost]
        [AllowAnonymous]
        public ActionResult UpdateUser(User_Accounts user)
        {
            string errorMessage = string.Empty;

            var existingUser = _userManager.GetUserById(user.id);
            if (existingUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("UserAccounts", "Admin");
            }

            existingUser.username = user.username ?? existingUser.username;
            existingUser.password = user.password ?? existingUser.password;
            existingUser.email = user.email ?? existingUser.email;
            existingUser.status = user.status; 

            ErrorCode updateResult = _userManager.UpdateUser(existingUser, ref errorMessage);

            if (updateResult == ErrorCode.Success) 
            {
                TempData["SuccessMessage"] = "User updated successfully.";
                return RedirectToAction("UserAccounts", "Admin");
            }
            else
            {
                TempData["ErrorMessage"] = errorMessage ?? "Failed to update user.";
                return RedirectToAction("UserAccounts", "Admin");
            }
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

            string emailBody = "We regret to inform you that your account has been rejected. If you have any questions, please contact support.";
            string errorMessage = "";

            var mailManager = new MailManager();
            bool emailSent = mailManager.SendEmail(user.email, "Account Rejection Notice", emailBody, ref errorMessage);

            if (!emailSent)
            {
                ModelState.AddModelError(string.Empty, $"Failed to send rejection email: {errorMessage}");
            }

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

        [HttpPost]
        public ActionResult DeleteStore(int id)
        {
            var res = new Response();
            string errorMessage = string.Empty;

            try
            {
                res.code = (int)_storeManager.DeleteStore(id, ref errorMessage);
                res.message = res.code == 1 ? "User deleted successfully." : errorMessage;
            }
            catch (Exception ex)
            {
                res.code = 0;
                res.message = $"An error occurred: {ex.Message}";
            }

            return Json(res);
        }
    }
}
