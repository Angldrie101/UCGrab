using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;
using UCGrab.Utils;

namespace UCGrab.Repository
{
    public class FavoritesManager
    {
        public String Message = String.Empty;
        //
        UCGrabEntities _db;
        UserManager _userMgr;
        BaseRepository<Product> _product;
        BaseRepository<Favorites> _fav;
        ProductManager _productManager;
        Favorites _favorites;

        public FavoritesManager()
        {
            _db = new UCGrabEntities();
            _userMgr = new UserManager();
            _product = new BaseRepository<Product>();
            _fav = new BaseRepository<Favorites>();
            _productManager = new ProductManager();
            _favorites = new Favorites();
        }
        public Favorites GetProductById(int? id)
        {
            return _fav.Get(id);
        }
        public List<Favorites> GetAllProduct(String userId)
        {
            return _fav._table.Where(m => m.user_id == userId).ToList();
        }

        public ErrorCode RemoveFromFavorites(string userId, int productId, ref string error)
        {
            var favorite = _db.Favorites.FirstOrDefault(f => f.user_id == userId && f.product_id == productId);
            if (favorite == null)
            {
                error = "Favorite item not found.";
                return ErrorCode.Error;
            }

            _db.Favorites.Remove(favorite);
            _db.SaveChanges();

            return ErrorCode.Success;
        }


    }
}