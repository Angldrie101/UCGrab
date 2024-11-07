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
                Total = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity)
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
            var order = _db.Order.FirstOrDefault(o => o.order_id == orderId);

            if (order != null)
            {
                order.order_status = (Int32)OrderStatus.ReadyToDeliver;
                _db.SaveChanges();

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        [AllowAnonymous]
        public ActionResult ToDeliver()
        {
            return View();
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