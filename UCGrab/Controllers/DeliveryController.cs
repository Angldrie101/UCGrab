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