using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using UCGrab.Database;
using UCGrab.Models;
using UCGrab.Utils;

namespace UCGrab.Controllers
{
    [Authorize(Roles = "DeliveryMan")]
    public class DeliveryController : BaseController
    {
        [AllowAnonymous]
        // GET: Delivery
        public ActionResult Orders()
        {
            IsUserLoggedSession();

            var orders = _orderManager.GetAllOrders();

            System.Diagnostics.Debug.WriteLine($"Total Orders count: {orders.Count}");

            if (orders.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("No orders found.");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Order IDs: {string.Join(", ", orders.Select(o => o.order_id))}");
            }

            var model = orders.Select(order => new OrderViewModel
            {
                OrderId = order.order_id,
                OrderNumber = order.order_id.ToString(),
                Building = order.building,
                Room = order.room,
                Firstname = order.firstname,
                Lastname = order.lastname,
                Stores = order.Store.store_name,
                StoreAddress = order.Store.store_address,
                Products = order.Order_Detail.Select(od => new ProductViewModel
                {
                    Quantity = (Int32)od.quatity,
                    ProductName = od.Product.product_name,
                    Price = (decimal)od.price,
                    ImageFilePath = od.Product.Image_Product.FirstOrDefault()?.image_file
                }).ToList(),
                Total = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity),
                DeliveryUserId = order.delivery_id
            }).ToList();

            return View(model);
        }
        [AllowAnonymous]
        public ActionResult OrderDetails(int id)
        {
            var order = _orderManager.GetOrderbyId(id);

            if (order == null)
            {
                return HttpNotFound();
            }

            var orderViewModel = new OrderViewModel
            {
                OrderId = order.order_id,
                Firstname = order.firstname,
                Lastname = order.lastname,
                Building = order.building,
                Room = order.room,
                Products = order.Order_Detail.Select(od => new ProductViewModel
                {
                    ProductId = (int)od.Product.id,
                    ProductName = od.Product.product_name,
                    Quantity = (int)od.quatity,
                    Price = (Int32)od.price
                }).ToList(),
                Total = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity),
                StoreImageUrl = "/path/to/store/image.jpg"
            };

            return View(orderViewModel);
        }
        public ActionResult GetOrderDetails(int orderId)
        {
            var order = _orderManager.GetOrderbyId(orderId);
            if (order != null)
            {
                return Json(new
                {
                    success = true,
                    orderId = order.order_id,
                    customerName = order.firstname + " " + order.lastname,
                    address = order.building + " at room " + order.room,
                    totalAmount = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity),
                    itemsCount = order.Products.Count,
                    products = order.Products
                });
            }
            return Json(new { success = false });
        }


        [AllowAnonymous]
        [HttpPost]
        public JsonResult ChangeOrderStatus(int orderId)
        {
            var _db = new UCGrabEntities();
            
            var currentUserIdStr = User.Identity.Name;
            
            var user = _userManager.GetUserInfoByUsername(currentUserIdStr);
            if (user == null)
            {
                System.Diagnostics.Debug.WriteLine("User authentication failed - user not found.");
                return Json(new { success = false, message = "User not authenticated." });
            }

            var order = _db.Order.FirstOrDefault(o => o.order_id == orderId);
            if (order == null)
            {
                System.Diagnostics.Debug.WriteLine($"Order with ID {orderId} not found in the database.");
                return Json(new { success = false, message = "Order not found." });
            }

            try
            {
                order.order_status = (int)OrderStatus.ReadyToDeliver;
                order.delivery_id = user.user_id;
                
                _db.SaveChanges();

                System.Diagnostics.Debug.WriteLine("Order status updated successfully.");
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating order status for Order ID {orderId}: {ex.Message}");
                return Json(new { success = false, message = "An error occurred while updating the order status." });
            }
        }

        [AllowAnonymous]
        public ActionResult ToDeliver()
        {
            IsUserLoggedSession();

            var userId = UserId;
            System.Diagnostics.Debug.WriteLine($"Logged in UserId: {userId}");
            
            var userInfo = _userManager.GetUserInfoByUserId(userId);
            if (userInfo == null)
            {
                return HttpNotFound("User not found.");
            }
            var orders = _orderManager.GetOrdersByDeliveryId(userInfo.user_id);
            System.Diagnostics.Debug.WriteLine($"Orders count: {orders.Count}");

            if (orders.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"No orders found for DeliveryId: {userInfo.user_id}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Order IDs: {string.Join(", ", orders.Select(o => o.order_id))}");
            }
            
            var model = orders.Select(order =>
            {
                var store = _storeManager.GetStoreById(order.store_id);

                return new OrderViewModel
                {
                    OrderId = order.order_id,
                    OrderNumber = order.order_id.ToString(),
                    OrderDate = order.order_date.HasValue ? order.order_date.Value : DateTime.MinValue,
                    Status = ((OrderStatus)order.order_status).ToString(),
                    Firstname = order.firstname,
                    Lastname = order.lastname,
                    Phone = order.phone,
                    Building = order.building,
                    Room = order.room,
                    AdditionalInfo = order.additional_info,
                    StoreAddress = order.Store.store_address,
                    Products = order.Order_Detail.Select(od => new ProductViewModel
                    {
                        ProductName = od.Product.product_name,
                        Quantity = (Int32)od.quatity,
                        Price = (Int32)od.price,
                        ImageFilePath = od.Product.Image_Product.FirstOrDefault()?.image_file
                    }).ToList(),
                    Total = (decimal)order.Order_Detail.Sum(od => od.price * od.quatity),
                    PaymentMethod = (Int32)order.payment_method,
                    Stores = store.store_name
                };
            }).ToList();

            return View(model);

        }

        [HttpPost]
        [Authorize]
        public ActionResult Delivered(int orderId)
        {
            var result = _orderManager.ToDeliverOrder(orderId);

            if (result == ErrorCode.Success)
            {
                return RedirectToAction("ToDeliver");
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Unable to confirm the order.");
            }
        }


        [AllowAnonymous]
        public ActionResult Logout()
        {
            Session.Clear();
            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "Home");
        }


        [Authorize]
        public ActionResult MyProfile()
        {
            IsUserLoggedSession();
            var user = User.Identity.Name;
            var userinfo = _userManager.GetUserInfoByUsername(user);

            if (userinfo == null)
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
                if (user == null)
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
                        if (_imageManager.UpdateImg(existingImage, ref ErrorMessage) == ErrorCode.Error)
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

    }
}