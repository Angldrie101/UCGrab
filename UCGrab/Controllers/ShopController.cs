using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using UCGrab.Database;
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

        [AllowAnonymous]
        public ActionResult MyProfile()
        {
            IsUserLoggedSession();
            var user = _userManager.CreateOrRetrieve(User.Identity.Name, ref ErrorMessage);

            return View(user);
        }

        [HttpPost]
        public ActionResult MyProfile(User_Information userInf)
        {
            if (_userManager.UpdateUserInformation(userInf, ref ErrorMessage) == Utils.ErrorCode.Error)
            {
                //
                ModelState.AddModelError(String.Empty, ErrorMessage);
                //
                return View(userInf);
            }
            TempData["Message"] = $"User Information {ErrorMessage}!";
            return View(userInf);
        }

        [Authorize]
        public ActionResult MyStore()
        {
            IsUserLoggedSession();
            var user = User.Identity.Name;
            var store = _storeManager.GetStoreByUserId(user);

            if (store == null)
            {
                TempData["ErrorMessage"] = "Store not found.";
            }

            return View(store);
        }

        [HttpPost]
        public ActionResult MyStore(Store store, User_Information ui)
        {
            IsUserLoggedSession();

            if (ModelState.IsValid)
            {

                var st = _storeManager.GetStoreByUserId(store.user_id);
                if (st == null)
                {
                    TempData["ErrorMessage"] = "User Not Found,";
                    return View(store);
                }

                // Update store details if needed
                st.operating_hours = store.operating_hours;
                st.store_description = store.store_description;

                if (_storeManager.StoreUpdate(store, ref ErrorMessage) != ErrorCode.Success)
                {
                    ModelState.AddModelError(String.Empty, ErrorMessage);
                    return View(store);
                }

                TempData["SuccessMessage"] = "Store information updated successfully.";
                return RedirectToAction("MyStore");
            }
            return View(store);
        }

        [Authorize]
        public ActionResult ProductInfo()
        {
            ViewBag.Category = Utilities.SelectListItemCategoryByUser(Username);
            ViewBag.Categories = _categoryManager.ListCategory(Username) ?? new List<Category>(); // Ensure it is not null
            ViewBag.Products = _productManager.ListProduct(Username) ?? new List<Product>(); // Ensure it is not null
            return View();
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
            ViewBag.Categories = _categoryManager.ListCategory(Username) ?? new List<Category>(); // Ensure it is not null
            ViewBag.Products = _productManager.ListProduct(Username) ?? new List<Product>(); // Ensure it is not null
            return View("ProductInfo");
        }

        [HttpPost]
        public ActionResult ProductInfo(Product product, HttpPostedFileBase[] files)
        {
            ViewBag.Category = Utilities.SelectListItemCategoryByUser(Username);
            ViewBag.Categories = _categoryManager.ListCategory(Username) ?? new List<Category>(); // Ensure it is not null
            ViewBag.Products = _productManager.ListProduct(Username) ?? new List<Product>();

            // Generate Unique Id
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
                // Iterating through multiple file collection   
                foreach (HttpPostedFileBase file in files)
                {
                    // Checking file is available to save.  
                    if (file != null)
                    {
                        var InputFileName = Path.GetFileName(file.FileName);
                        if (!Directory.Exists(Server.MapPath("~/UploadedFiles/")))
                            Directory.CreateDirectory(Server.MapPath("~/UploadedFiles/"));

                        var ServerSavePath = Path.Combine(Server.MapPath("~/UploadedFiles/") + InputFileName);
                        // Save file to server folder  
                        file.SaveAs(ServerSavePath);

                        Image_Product imgproduct = new Image_Product
                        {
                            image_file = InputFileName,
                            product_id = product.id
                        };

                        if (_imageManager.CreateImgProduct(imgproduct, ref ErrorMessage) == ErrorCode.Error)
                        {
                            ModelState.AddModelError(String.Empty, ErrorMessage);
                            // Remove created product and set error 
                            _productManager.DeleteProduct(product.id, ref ErrorMessage);
                            // Remove created image attachment
                            _imageManager.DeleteImgByProductId(product.id, ref ErrorMessage);
                            return View(product);
                        }
                    }
                }
            }

            TempData["Message"] = $"Product {product.product_name} added!";
            return RedirectToAction("ProductInfo");
        }
    }
}