using System;
using System.Collections.Generic;
using System.Linq;
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

            var orders = _orderManager.GetAllOrders(); // Retrieve all orders without filtering by user

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
                Total = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity)
            }).ToList();

            return View(model);
        }
        [AllowAnonymous]
        public ActionResult OrderDetails(int id)
        {
            // Assuming you have a method to get the full order details by order ID
            var order = _orderManager.GetOrderbyId(id);

            if (order == null)
            {
                return HttpNotFound();
            }

            // Map the Order entity to OrderViewModel
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
                    // other properties if needed
                }).ToList(),

                // Add other necessary mappings, like the store image URL
                StoreImageUrl = "/path/to/store/image.jpg"  // Example, change as needed
            };

            // Return the view with the view model
            return View(orderViewModel);
        }

        [AllowAnonymous]
        [HttpPost]
        public JsonResult ChangeOrderStatus(int orderId)
        {
            var _db = new UCGrabEntities();
            var order = _db.Order.FirstOrDefault(o => o.order_id == orderId);

            if (order != null)
            {
                // Assuming "Confirmed" is the new status
                order.order_status = (Int32)OrderStatus.ReadyToDeliver;
                _db.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false });
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