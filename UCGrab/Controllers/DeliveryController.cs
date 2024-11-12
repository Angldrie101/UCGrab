using System;
using System.Collections.Generic;
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
                Products = order.Order_Detail.Select(od => new ProductViewModel
                {
                    Quantity = (Int32)od.quatity,
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
                Products = order.Products.Select(p => new ProductViewModel
                {
                    ProductId = (Int32)p.id,
                    Total = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity),
                    Price = p.price,
                }).ToList(),
                StoreImageUrl = "/path/to/store/image.jpg" 
            };
            return View(orderViewModel);
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult ChangeOrderStatus(int orderId)
        {
            var _db = new UCGrabEntities();

            // Retrieve the current user's ID as a string from their identity
            var currentUserIdStr = User.Identity.Name;

            System.Diagnostics.Debug.WriteLine($"Attempting to change order status for Order ID: {orderId}");
            System.Diagnostics.Debug.WriteLine($"Current User ID (from Identity): {currentUserIdStr}");

            // Use the GetUserInfoByUserId function to get user information
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
                // Update order status and assign the current user's ID as the delivery_id
                order.order_status = (int)OrderStatus.ReadyToDeliver;
                order.delivery_id = user.user_id;

                // Save changes to the database
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

            // Get the user ID of the currently logged-in user
            var userId = UserId;
            System.Diagnostics.Debug.WriteLine($"Logged in UserId: {userId}");

            // Retrieve user information based on the user ID
            var userInfo = _userManager.GetUserInfoByUserId(userId);
            if (userInfo == null)
            {
                return HttpNotFound("User not found.");
            }

            // Get orders associated with the delivery_id matching the logged-in user
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

            // Fetch store information by store_id and include store name
            var model = orders.Select(order =>
            {
                // Fetch the store by store_id
                var store = _storeManager.GetStoreById(order.store_id);  // Assuming GetStoreById is a method in the store manager

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
        
    }
}