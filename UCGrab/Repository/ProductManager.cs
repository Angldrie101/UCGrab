using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using UCGrab.Database;
using UCGrab.Models;
using UCGrab.Utils;

namespace UCGrab.Repository
{
    public class ProductManager
    {
        UserManager _userMgr;
        BaseRepository<Product> _product;
        BaseRepository<Stock> _stock;
        public ProductManager()
        {
            _userMgr = new UserManager();
            _product = new BaseRepository<Product>();
            _stock = new BaseRepository<Stock>();
        }
        public List<Product> ListActiveProduct(String storeId)
        {
            using (var context = new UCGrabEntities())
            {
                return context.Product
                    .Where(p => p.user_id == storeId && p.status == (int)ProductStatus.HasStock).Include(p => p.Image_Product).ToList();
            }
        }

        public List<Product> ListAll()
        {
            return _product._table.Where(m => m.status == (Int32)ProductStatus.HasStock).ToList();
        }
        public List<Product> ListProduct(String username)
        {
            var user = _userMgr.GetUserByUsername(username);
            return _product._table.Where(m => m.user_id == user.user_id).ToList();
        }

        public Product GetProductById(int? id)
        {
            return _product.Get(id);
        }

        public Product GetProductBygUId(String gUid)
        {
            return _product._table.Where(m => m.product_id == gUid).FirstOrDefault();
        }

        public Product GetProductInfo(String productId)
        {
            return _product._table.Where(m => m.product_id == productId).FirstOrDefault();
        }

        public ErrorCode CreateProduct(Product prod, ref String err)
        {
            return _product.Create(prod, out err);
        }

        public ErrorCode DeleteProduct(int? id, ref String error)
        {
            return _product.Delete(id, out error);
        }
        public ErrorCode AddStock(Stock s, ref String err)
        {
            return _stock.Create(s, out err);
        }

        public ErrorCode UpdateProduct(Product product, ref string errorMessage)
        {
            try
            {
                using (var db = new UCGrabEntities())
                {
                    db.Entry(product).State = EntityState.Modified;
                    db.SaveChanges();
                }
                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return ErrorCode.Error;
            }
        }

        public int GetTotalProducts(string userId)
        {
            return _product._table.Count(p => p.user_id == userId && p.status == (int)ProductStatus.HasStock);
        }

        public List<Product> GetProductsByStoreId(int storeId)
        {
            try
            {
                // Retrieve all products associated with the given store_id
                return _product._table.Where(p => p.store_id == storeId).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while retrieving products for the store.", ex);
            }
        }
        public IEnumerable<Product> ListActiveProductByCategory(string userId, int categoryId)
        {
            var _db = new UCGrabEntities();
            return _db.Product.Where(product => product.user_id == userId && product.category_id == categoryId && product.status==(int)Status.Active).ToList();
        }


        public List<ProductViewModel> GetTopSellingProducts(string userId)
        {
            var _db = new UCGrabEntities();
            return _db.Order_Detail
                      .Where(od => od.Order.user_id == userId)
                      .GroupBy(od => od.product_id)
                      .Select(g => new ProductViewModel
                      {
                          ProductId = (Int32)g.Key,
                          ProductName = g.FirstOrDefault().Product.product_name,
                          Quantity = (Int32)g.Sum(od => od.quatity),
                          Total = (Int32)g.Sum(od => od.price * od.quatity)
                      })
                      .OrderByDescending(p => p.Quantity)
                      .Take(10)
                      .ToList();
        }
    }
}