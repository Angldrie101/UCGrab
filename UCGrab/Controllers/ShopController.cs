﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using UCGrab.Database;
using UCGrab.Models;
using UCGrab.Repository;
using UCGrab.Utils;
using OfficeOpenXml;
using iTextSharp.text;
using iTextSharp.text.pdf;

namespace UCGrab.Controllers
{
    [Authorize(Roles = "Provider")]
    public class ShopController : BaseController
    {
        [AllowAnonymous]
        // GET: Shop
        public ActionResult Index()
        {
            IsUserLoggedSession();

            var userid = User.Identity.Name;
            var userInfo = _userManager.GetUserInfoByUsername(userid);
            var _db = new UCGrabEntities();
            
            var totalSales = _db.Order_Detail
                                .Where(od => od.Order.store_id == userInfo.store_id && od.Order.order_status == (int)OrderStatus.Delivered)
                                .Sum(od => od.price)??0;
            
            var totalOrders = _db.Order
                                  .Where(o => o.store_id == userInfo.store_id && o.order_status == (int)OrderStatus.Pending)
                                  .Count();
            
            var totalProducts = _db.Product
                                    .Where(p => p.Store.id == userInfo.store_id)
                                    .Count();

            var newCustomer = _db.Order
                                    .Where(p => p.Store.id == userInfo.store_id && p.order_status == (int)OrderStatus.Pending)
                                    .Count();
            var recentOrders = _db.Order
                                    .Where(o => o.store_id == userInfo.store_id && o.order_status == (int)OrderStatus.Delivered)
                                    .OrderByDescending(o => o.order_id) 
                                    .Take(5)
                                    .ToList();

            var reviews = _db.Review
                     .Where(r => r.Order.store_id == userInfo.store_id)
                     .Select(r => new ReviewModel
                     {
                         OrderId = r.Order.order_id,
                         FirstName = r.User_Information.first_name,
                         LastName = r.User_Information.last_name,
                         Rating = r.rating,
                         Comment = r.comment,
                         ReviewDate = r.review_date
                     })
                     .ToList();
            var dashboardData = new ProviderDashboardViewModel
            {
                TotalSales = totalSales,
                TotalOrders = totalOrders,
                TotalProducts = totalProducts,
                NewCustomers = newCustomer,
                RecentOrders = recentOrders,
                Reviews = reviews
            };

            return View(dashboardData);
        }

        public ActionResult GenerateReports()
        {
            var userId = User.Identity.Name;
            var userInfo = _userManager.GetUserInfoByUsername(userId);
            var _db = new UCGrabEntities();

            // Get total sales
            var totalSales = _db.Order_Detail
                                .Where(od => od.Order.store_id == userInfo.store_id && od.Order.order_status == (int)OrderStatus.Delivered)
                                .Sum(od => od.price) ?? 0;

            // Get orders
            var orders = _db.Order
                            .Where(o => o.store_id == userInfo.store_id)
                            .Select(o => new OrderViewModel
                            {
                                OrderId = o.order_id,
                                OrderDate = o.order_date,
                                OrderStatus = o.order_status,
                                PaymentMethod = o.payment_method,
                                CheckOutOption = o.checkOut_option,
                                DeliveryUserId = o.delivery_id,
                                Firstname = o.firstname,
                                Lastname = o.lastname
                            })
                            .ToList();

            // Get products
            var products = _db.Product
                              .Where(p => p.Store.id == userInfo.store_id)
                              .Select(p => new ProductViewModel
                              {
                                  ProductId = p.id,
                                  ProductName = p.product_name,
                                  Price = p.price,
                              })
                              .ToList();
            

            var store = _db.Store
                        .Where(s => s.id == userInfo.store_id)
                       .Select(s => s.store_name)
                       .FirstOrDefault();

            // Create a model to pass to the view
            var model = new ReportViewModel
            {
                TotalSales = totalSales,
                Orders = orders,
                Products = products,
                GeneratedDate = DateTime.Now,
                StoreName = store
            };

            // Render the view as PDF
            var pdf = new Rotativa.ViewAsPdf("GenerateReports", model)
            {
                FileName = $"StoreReports_{DateTime.Now:yyyyMMddHHmmss}.pdf",
                PageSize = Rotativa.Options.Size.A4,
                PageMargins = new Rotativa.Options.Margins(25, 25, 30, 30)
            };

            return pdf;
        }

        [Authorize]
        public ActionResult ListOrders()
        {
            IsUserLoggedSession();

            var userId = UserId; 
            System.Diagnostics.Debug.WriteLine($"Logged in UserId: {userId}");

            var store = _storeManager.GetStoreByUserId(userId);

            ViewBag.ToConfirmCount = store.Order.Count(o => o.order_status == 1);
            ViewBag.ToDeliverCount = store.Order.Count(o => o.order_status == 3);
            ViewBag.ToReceiveCount = store.Order.Count(o => o.order_status == 4);
            ViewBag.DeliveredCount = store.Order.Count(o => o.order_status == 5);
            ViewBag.CancelledCount = store.Order.Count(o => o.order_status == 2);
            ViewBag.RejectedCount = store.Order.Count(o => o.order_status == 7);

            if (store == null)
            {
                return HttpNotFound("Store not found.");
            }

            var storeId = Convert.ToInt32(store.id); 
            System.Diagnostics.Debug.WriteLine($"StoreId: {storeId}");

            var orders = _orderManager.GetOrdersByStoreId(storeId); 
            System.Diagnostics.Debug.WriteLine($"Orders count: {orders.Count}");

            if (orders.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine($"No orders found for StoreId: {storeId}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Order IDs: {string.Join(", ", orders.Select(o => o.order_id))}");
            }

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
                    ImageFilePath = od.Product.Image_Product.FirstOrDefault()?.image_file
                }).ToList(),
                Total = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity),
                GCashReceipt = order.gcash_receipt 

            }).ToList();

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public ActionResult ConfirmOrder(int orderId)
        {
            var result = _orderManager.ConfirmOrder(orderId);

            if (result == ErrorCode.Success)
            {
                return RedirectToAction("ListOrders");
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Unable to confirm the order.");
            }
        }
        [HttpPost]
        [Authorize]
        public ActionResult RejectOrder(int orderId)
        {
            var result = _orderManager.RejectOrder(orderId);

            if (result == ErrorCode.Success)
            {
                var _db = new UCGrabEntities();
                var order = _db.Order.FirstOrDefault(o => o.order_id == orderId);

                if (order != null)
                {
                    var customer = _db.User_Accounts.FirstOrDefault(u => u.user_id == order.user_id);
                    if (customer != null)
                    {
                        string emailBody = "We regret to inform you that your order has been rejected. If you have any questions, please contact support. Your payment will be refunded to your account.";
                        string errorMessage = "";

                        var mailManager = new MailManager();
                        bool emailSent = mailManager.SendEmail(customer.email, "Order Rejection Notice", emailBody, ref errorMessage);

                        if (!emailSent)
                        {
                            ModelState.AddModelError(string.Empty, $"Failed to send rejection email: {errorMessage}");
                        }
                    }
                }

                return RedirectToAction("ListOrders");
            }
            else
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Unable to reject the order.");
            }
        }

        [AllowAnonymous]
        public ActionResult MyProfile()
        {
            IsUserLoggedSession();

            var user = User.Identity.Name;
            var usrinfo = _userManager.GetUserInfoByUsername(user);

            if (usrinfo == null)
            {
                TempData["ErrorMessage"] = "Failed retrieving user information.";
                return RedirectToAction("MyProfile", "Shop");
            }

            return View(usrinfo);
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
                        existingImage.image_file = profileFileName;
                        if (_imageManager.UpdateImg(existingImage, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            return View(userInf);
                        }
                    }
                    else
                    {
                        UCGrab.Database.Image img = new UCGrab.Database.Image
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
                return RedirectToAction("DisplayProfile", "Shop");
            }

            return View(userInf);
        }

        public ActionResult DisplayProfile()
        {
            IsUserLoggedSession();
            var user = User.Identity.Name;
            var userinfo = _userManager.GetUserInfoByUsername(user);

            if (userinfo == null)
            {
                TempData["ErrorMessage"] = "Failed retreiving user information.";
                return RedirectToAction("MyProfile", "Shop");

            }
            return View(userinfo);
        }
        [HttpPost]
        public ActionResult DisplayProfile(User_Information userInf, HttpPostedFileBase profilePicture)
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
        public ActionResult MyStore()
        {
            var store = _storeManager.CreateOrRetrieve(User.Identity.Name, ref ErrorMessage);

            return View(store);
        }

        [HttpPost]
        public ActionResult MyStore(Store store, HttpPostedFileBase storeLogo)
        {
            IsUserLoggedSession();

            if (ModelState.IsValid)
            {
                var user = _userManager.GetUserByUserId(store.store_id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User Not Found,";
                    return View(store);
                }
            }
            if (storeLogo != null && storeLogo.ContentLength > 0)
            {
                var uploadsFolderPath = Server.MapPath("~/UploadedFiles/");
                if (!Directory.Exists(uploadsFolderPath))
                    Directory.CreateDirectory(uploadsFolderPath);

                var profileFileName = Path.GetFileName(storeLogo.FileName);
                var profileSavePath = Path.Combine(uploadsFolderPath, profileFileName);
                storeLogo.SaveAs(profileSavePath);

                var existingImage = _imageManager.ListImgAttachByImageStoreId(store.id).FirstOrDefault();
                if (existingImage != null)
                {
                    existingImage.image_file = profileFileName;
                    if (_imageManager.UpdateImgStore(existingImage, ref ErrorMessage) == ErrorCode.Error)
                    {
                        ModelState.AddModelError(String.Empty, ErrorMessage);
                        return View(store);
                    }
                }

            }
            return View(store);
        }

        [Authorize]
        public ActionResult ChangeLogo()
        {
            var store = _storeManager.CreateOrRetrieve(User.Identity.Name, ref ErrorMessage);

            return View(store);
        }

        [HttpPost]
        public ActionResult ChangeLogo(Store store, HttpPostedFileBase profilePicture, HttpPostedFileBase qr)
        {
            IsUserLoggedSession();

            if (ModelState.IsValid)
            {
                var user = _userManager.GetUserByUserId(store.user_id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Error updating profile: User not found.";
                    return View(store);
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

                    var existingImage = _imageManager.ListImgAttachByImageStoreId(store.id).FirstOrDefault();
                    if (existingImage != null)
                    {
                        existingImage.image_file = profileFileName;
                        if (_imageManager.UpdateImgStore(existingImage, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            return View(store);
                        }
                    }
                    else
                    {
                        Image_Store img = new Image_Store
                        {
                            image_file = profileFileName,
                            store_id = store.id
                        };

                        if (_imageManager.CreateImgStore(img, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            return View(store);
                        }
                    }
                }
                if (qr != null && qr.ContentLength > 0)
                {
                    var uploadsFolderPath = Server.MapPath("~/UploadedFiles/");
                    if (!Directory.Exists(uploadsFolderPath))
                        Directory.CreateDirectory(uploadsFolderPath);

                    var qrfileName = Path.GetFileName(qr.FileName);
                    var qrSavePath = Path.Combine(uploadsFolderPath, qrfileName);
                    qr.SaveAs(qrSavePath);


                    System.Diagnostics.Debug.WriteLine("Profile picture saved at: " + qrSavePath);

                    var existingImage = _imageManager.ListImgAttachByImageStoreId(store.id).FirstOrDefault();
                    if (existingImage != null)
                    {
                        existingImage.qr_file = qrfileName;
                        if (_imageManager.UpdateImgStore(existingImage, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            return View(store);
                        }
                    }
                    else
                    {
                        Image_Store img = new Image_Store
                        {
                            store_id = store.id,
                            qr_file = qrfileName
                        };

                        if (_imageManager.CreateImgStore(img, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            return View(store);
                        }
                    }
                }
                if (_storeManager.UpdateStore(store.id, store, ref ErrorMessage) == ErrorCode.Error)
                {
                    ModelState.AddModelError(String.Empty, ErrorMessage);
                    return View(store);

                }
                TempData["SuccessMessage"] = "Profile updated successfully.";
                return RedirectToAction("MyStore");
            }

            return View(store);
        }

        [Authorize]
        public ActionResult ProductInfo()
        {
            ViewBag.Categories = UCGrab.Utils.Utilities.GetAllCategory;

            var products = _productManager.ListProduct(Username) ?? new List<Product>();
            var productViewModels = products.Select(product => new ProductViewModel
            {
                Product = product,
                TotalStock = product.Stock.Sum(s => s.quantity)
            }).ToList();

            ViewBag.Products = productViewModels;

            return View(productViewModels);
        }

        [HttpPost]
        public ActionResult ProductInfo(Stock stocks,Product product, HttpPostedFileBase files, int stock_quantity, int Category, string size)
        {
            ViewBag.Categories = UCGrab.Utils.Utilities.GetAllCategory;
            var products = _productManager.ListProduct(Username) ?? new List<Product>();
            var productViewModels = products.Select(p => new ProductViewModel
            {
                Product = p,
                TotalStock = p.Stock.Sum(s => s.quantity)
            }).ToList();
            ViewBag.Products = productViewModels;

            if (stock_quantity <= 0)
            {
                TempData["ErrorMessage"] = "Product stock should be greater than 0.";
                return View(productViewModels);
            }

            var user = _userManager.GetUserInfoByUsername(Username);
            var storeId = _storeManager.GetStoreByUserId(user.user_id);

            if (products.Any(p => p.product_name.Equals(product.product_name, StringComparison.OrdinalIgnoreCase) && p.store_id == storeId.id))
            {
                TempData["ErrorMessage"] = $"Product is already in your store.";
                return View(productViewModels);
            }

            var prodgUid = $"Item-{UCGrab.Utils.Utilities.gUid}";
            product.product_id = prodgUid;
            product.user_id = user.user_id;
            product.size = string.Join(",", size.Split(',').Distinct());
            product.date_created = DateTime.Now;
            product.status = (Int32)ProductStatus.NoStock;
            product.store_id = storeId.id;
            product.category_id = Category;

            if (_productManager.CreateProduct(product, ref ErrorMessage) == ErrorCode.Error)
            {
                ModelState.AddModelError(String.Empty, ErrorMessage);
                return View(productViewModels);
            }

            product = _productManager.GetProductBygUId(prodgUid);

            Stock stock = new Stock
            {
                product_id = product.id,
                quantity = stock_quantity
            };

            if (_productManager.AddStock(stock, ref ErrorMessage) == ErrorCode.Error)
            {
                ModelState.AddModelError(String.Empty, ErrorMessage);
                _productManager.DeleteProduct(product.id, ref ErrorMessage);
                _imageManager.DeleteImgByProductId(product.id, ref ErrorMessage);
                return View(productViewModels);
            }

            product.status = (stock_quantity > 0) ? (Int32)ProductStatus.HasStock : (Int32)ProductStatus.NoStock;
            if (_productManager.UpdateProduct(product, ref ErrorMessage) == ErrorCode.Error)
            {
                ModelState.AddModelError(String.Empty, ErrorMessage);
                return View(productViewModels);
            }

            if (files != null)
            {
                var InputFileName = Path.GetFileName(files.FileName);
                if (!Directory.Exists(Server.MapPath("~/UploadedFiles/")))
                    Directory.CreateDirectory(Server.MapPath("~/UploadedFiles/"));

                var ServerSavePath = Path.Combine(Server.MapPath("~/UploadedFiles/") + InputFileName);
                files.SaveAs(ServerSavePath);

                Image_Product imgproduct = new Image_Product
                {
                    image_file = InputFileName,
                    product_id = product.id
                };

                if (_imageManager.CreateImgProduct(imgproduct, ref ErrorMessage) == ErrorCode.Error)
                {
                    ModelState.AddModelError(String.Empty, ErrorMessage);
                    _productManager.DeleteProduct(product.id, ref ErrorMessage);
                    _imageManager.DeleteImgByProductId(product.id, ref ErrorMessage);
                    return View(productViewModels);
                }
            }

            TempData["Message"] = $"Product {product.product_name} added!";
            return RedirectToAction("ProductInfo");
        }
        
        public JsonResult ProductDelete(int? id)
        {
            var res = new Response();
            res.code = (Int32)_productManager.DeleteProduct(id, ref ErrorMessage);
            res.message = ErrorMessage;

            return Json(res, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult EditProduct(int productId, int addStock)
        {
            var product = _productManager.GetProductById(productId);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction("ProductInfo");
            }

            var existingStock = _productManager.GetStockByProductId(productId);
            if (existingStock != null)
            {
                existingStock.quantity += addStock;

                if (_productManager.UpdateStock(existingStock, ref ErrorMessage) == ErrorCode.Error)
                {
                    TempData["Error"] = ErrorMessage;
                    return RedirectToAction("ProductInfo");
                }
            }
            else
            {
                TempData["Error"] = "Stock entry not found for this product.";
                return RedirectToAction("ProductInfo");
            }

            product.status = (product.Stock.Sum(s => s.quantity) > 0) ? (int)ProductStatus.HasStock : (int)ProductStatus.NoStock;
            _productManager.UpdateProduct(product, ref ErrorMessage);

            TempData["SuccessMessage"] = "Product updated successfully.";
            return RedirectToAction("ProductInfo");
        }


        public JsonResult CategoryDelete(int? id)
        {
            var res = new Response();
            res.code = (Int32)_categoryManager.DeleteCategory(id, ref ErrorMessage);
            res.message = ErrorMessage;

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ProductStockAdd(int id, int qty)
        {
            Stock stock = new Stock
            {
                product_id = id,
                quantity = qty
            };

            var res = new Response();

            if (qty == 0)
            {
                res.code = (Int32)ErrorCode.Error;
                res.message = "Quantity Not Valid!";

                return Json(res, JsonRequestBehavior.AllowGet);
            }

            res.code = (Int32)_productManager.AddStock(stock, ref ErrorMessage);
            res.message = ErrorMessage;

            return Json(res, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DiscountVouchers()
        {
            var _db = new UCGrabEntities();

            var username = User.Identity.Name;
            var userInfo = _userManager.GetUserInfoByUsername(username);
            var products = _productManager.GetProductByStoreId(userInfo.store_id);

            ViewBag.CurrentStoreId = userInfo.store_id;
            ViewBag.ProductByStoreId = products;

            var discountList = _db.Discounts.ToList(); 
            var voucherList = _db.Vouchers.ToList();  

            var viewModel = new DiscountVoucherViewModel
            {
                Discounts = discountList,
                Vouchers = voucherList
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DiscountVouchers(Discounts model,DateTime startDate,DateTime endDate, int? ProductId, string DiscountType, decimal DiscountValue, decimal? MinOrderAmount)
        {
            var _db = new UCGrabEntities();
            var errorMessage = string.Empty;

            if (ModelState.IsValid)
            {
                var username = User.Identity.Name;
                var userInfo = _userManager.GetUserInfoByUsername(username);

                if (startDate > endDate)
                {
                    ModelState.AddModelError("", "Start date cannot be later than end date.");
                    return View(model);
                }

                try
                {
                    model.store_id = userInfo.store_id;
                    model.product_id = ProductId;
                    model.start_date = startDate;
                    model.end_date = endDate;
                    model.discount_type = DiscountType;
                    model.discount_value = DiscountValue;
                    model.min_product_amount = MinOrderAmount;

                    var existingDiscount = _discountManager.ListDiscountsByStoreId((int)userInfo.store_id)
                                .FirstOrDefault(d => d.product_id == ProductId);

                    if (existingDiscount != null)
                    {
                        if (startDate <= existingDiscount.end_date) 
                        {
                            TempData["Error"] = "New discount can only be added if the start date is beyond the existing discount's end date.";
                            return RedirectToAction("DiscountVouchers");
                        }
                    }

                    var result = _discountManager.AddDiscount(model, ref errorMessage);

                    if (result == ErrorCode.Success)
                    {
                        TempData["Success"] = "Discount added successfully!";
                        return RedirectToAction("DiscountVouchers");
                    }
                    else
                    {
                        TempData["Error"] = $"Error saving discount: {errorMessage}";
                        return RedirectToAction("DiscountVouchers");
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Error saving discount: {ex.Message}";
                    return RedirectToAction("DiscountVouchers");
                }
            }
            var usernames = User.Identity.Name;
            var userInfos = _userManager.GetUserInfoByUsername(usernames);

            var discountsList = _discountManager.ListDiscountsByStoreId((int)userInfos.store_id);
            var vouchersList = _discountManager.ListVoucherByStoreId((int)userInfos.store_id); 

            var viewModel = new DiscountVoucherViewModel
            {
                Discounts = discountsList,
                Vouchers = vouchersList
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddVoucher(Vouchers model, DateTime vstartDate, DateTime vendDate, string VocherCode, string VDiscountType, decimal VDiscountValue, decimal VMinOrderAmount, int VMaxUses)
        {
            var _db = new UCGrabEntities();
            var errorMessage = string.Empty;

            if (ModelState.IsValid)
            {
                var username = User.Identity.Name;
                var userInfo = _userManager.GetUserInfoByUsername(username);

                 bool voucherExists = _db.Vouchers.Any(v => v.voucher_code == VocherCode && v.store_id == userInfo.store_id);
                if (voucherExists)
                {
                    TempData["ErrorVoucher"] = "The voucher code already exists. Please use a different code.";
                    return RedirectToAction("DiscountVouchers");
                }

                if (vstartDate > vendDate)
                {
                    TempData["ErrorVoucher"] = "Start date cannot be later than end date.";
                    return RedirectToAction("DiscountVouchers");
                }

                try
                {
                    model.store_id = userInfo.store_id;
                    model.voucher_code = VocherCode;
                    model.start_date = vstartDate;
                    model.end_date = vendDate;
                    model.max_uses = VMaxUses;
                    model.discount_type = VDiscountType;
                    model.discount_value = VDiscountValue;
                    model.min_order_amount = VMinOrderAmount;

                    var result = _discountManager.AddVoucher(model, ref errorMessage);

                    if (result == ErrorCode.Success)
                    {
                        TempData["SuccessVoucher"] = "Voucher added successfully!";
                        return RedirectToAction("DiscountVouchers");
                    }
                    else
                    {
                        TempData["ErrorVoucher"] = $"Error saving voucher: {errorMessage}";
                        return RedirectToAction("DiscountVouchers");
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorVoucher"] = $"Error saving voucher: {ex.Message}";
                    return RedirectToAction("DiscountVouchers");
                }
            }
            var usernames = User.Identity.Name;
            var userInfos = _userManager.GetUserInfoByUsername(usernames);

            var discountsList = _discountManager.ListDiscountsByStoreId((int)userInfos.store_id);
            var vouchersList = _discountManager.ListVoucherByStoreId((int)userInfos.store_id);

            var viewModel = new DiscountVoucherViewModel
            {
                Discounts = discountsList,
                Vouchers = vouchersList
            };

            return View(viewModel);
        }

    }
}