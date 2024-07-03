using System;
using System.Collections.Generic;
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
        public List<Product> ListActiveProduct()
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
    }
}