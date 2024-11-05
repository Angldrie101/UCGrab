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
        public ActionResult OrderDetails()
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