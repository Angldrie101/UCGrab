using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using UCGrab.Database;
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
    }
}