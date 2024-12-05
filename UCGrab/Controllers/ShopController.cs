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
using UCGrab.Repository;
using UCGrab.Utils;

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

            return View();
        }

        [Authorize]
        public ActionResult ListOrders()
        {
            IsUserLoggedSession();

            var userId = UserId; 
            System.Diagnostics.Debug.WriteLine($"Logged in UserId: {userId}");

            var store = _storeManager.GetStoreByUserId(userId);
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
                Total = (Int32)order.Order_Detail.Sum(od => od.price * od.quatity)
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
            ViewBag.Category = Utilities.SelectListItemCategoryByUser(Username);
            ViewBag.Categories = _categoryManager.ListCategory(Username) ?? new List<Category>();

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
        public ActionResult AddCategory(string category_name)
        {
            if (string.IsNullOrEmpty(category_name))
            {
                ModelState.AddModelError("category_name", "Category name is required");
            }
            else
            {
                var category = new Category
                {
                    category_name = category_name,
                    date_created = DateTime.Now,
                    user_id = UserId
                };

                if (_categoryManager.CreateCategory(category, ref ErrorMessage) == ErrorCode.Error)
                {
                    ModelState.AddModelError(string.Empty, ErrorMessage);
                }
            }

            ViewBag.Category = Utilities.SelectListItemCategoryByUser(Username);
            ViewBag.Categories = _categoryManager.ListCategory(Username) ?? new List<Category>();
            var products = _productManager.ListProduct(Username) ?? new List<Product>();

            var productViewModels = products.Select(product => new ProductViewModel
            {
                Product = product,
                TotalStock = product.Stock.Sum(s => s.quantity)
            }).ToList();

            return View("ProductInfo", productViewModels);
        }

        [HttpPost]
        public ActionResult ProductInfo(Product product, HttpPostedFileBase[] files, int stock_quantity)
        {
            ViewBag.Category = Utilities.SelectListItemCategoryByUser(Username);
            ViewBag.Categories = _categoryManager.ListCategory(Username) ?? new List<Category>();
            ViewBag.Products = _productManager.ListProduct(Username) ?? new List<Product>();

            var prodgUid = $"Item-{Utilities.gUid}";
            product.product_id = prodgUid;
            product.user_id = UserId;
            product.date_created = DateTime.Now;
            product.status = (Int32)ProductStatus.NoStock;

            if (_productManager.CreateProduct(product, ref ErrorMessage) == ErrorCode.Error)
            {
                ModelState.AddModelError(String.Empty, ErrorMessage);
                return View(product);
            }

            product = _productManager.GetProductBygUId(prodgUid);

            if (ModelState.IsValid)
            {
                foreach (HttpPostedFileBase file in files)
                {
                    if (file != null)
                    {
                        var InputFileName = Path.GetFileName(file.FileName);
                        if (!Directory.Exists(Server.MapPath("~/UploadedFiles/")))
                            Directory.CreateDirectory(Server.MapPath("~/UploadedFiles/"));

                        var ServerSavePath = Path.Combine(Server.MapPath("~/UploadedFiles/") + InputFileName);
                        file.SaveAs(ServerSavePath);

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
                            return View(product);
                        }
                    }
                }
                
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
                    return View(product);
                }

                product.status = (stock_quantity > 0) ? (Int32)ProductStatus.HasStock : (Int32)ProductStatus.NoStock;
                if (_productManager.UpdateProduct(product, ref ErrorMessage) == ErrorCode.Error)
                {
                    ModelState.AddModelError(String.Empty, ErrorMessage);
                    return View(product);
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

    }
}