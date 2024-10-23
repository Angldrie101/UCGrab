using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UCGrab.Database;
using UCGrab.Models;

namespace UCGrab.Controllers
{
    [Authorize(Roles = "DeliveryMan")]
    public class DeliveryController : BaseController
    {
        [AllowAnonymous]
        // GET: Delivery
        public ActionResult Orders()
        {
            var orders = _orderManager.GetOrderByUserId(UserId);
            return View(orders);
        }
        [AllowAnonymous]
        public ActionResult OrderDetails()
        {
            return View();
        }
        
    }
}