using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;
using UCGrab.Utils;

namespace UCGrab.Repository
{
    public class StoreManager
    {
        BaseRepository<Store> _store;
        BaseRepository<User_Information> _userInfo;
        UserManager _userMgr;

        public StoreManager()
        {
            _store = new BaseRepository<Store>();
            _userInfo = new BaseRepository<User_Information>();
            _userMgr = new UserManager();
        }
        public List<Store> ListStore()
        {
            return _store._table.Where(m => m.status == (Int32)StoreStatus.Active).ToList();
        }
        public List<Store> ManageStore()
        {
            return _store._table.Where(m => m.status == (int)StoreStatus.Active || m.status == (int)StoreStatus.Inactive).ToList();
        }
        public Store GetStoreById(int? id)
        {
            return _store.Get(id);
        }
        public Store GetStoreId(int? storeId)
        {
            return _store._table.Where(s => s.id == storeId).FirstOrDefault();
        }
        public Store GetStoreByGuId(String id)
        {
            return _store.GetAll().Where(m => m.store_id == id).FirstOrDefault();
        }

      
        public Store GetStoreByUserId(string userId)
        {

            return _store._table.FirstOrDefault(m => m.user_id == userId);
        }
        public IEnumerable<Store> ListStoresByCategory(int categoryId)
        {
            var _db = new UCGrabEntities();
            return _db.Store.Where(store => store.Product.Any(product => product.category_id == categoryId)).ToList();
        }


        public ErrorCode AddStoreForUser(Store store, string user_id, ref String err)
        {
            var userInfo = _userInfo.GetAll().FirstOrDefault(ui => ui.user_id == user_id);
            if (userInfo == null)
            {
                err = "User not found.";
                return ErrorCode.Error;
            }

            store.store_id = Utilities.gUid;
            if (_store.Create(store, out err) != ErrorCode.Success)
            {
                return ErrorCode.Error;
            }

            userInfo.store_id = store.id;
            if (_userInfo.Update(userInfo.id, userInfo, out err) != ErrorCode.Success)
            {
                return ErrorCode.Error;
            }

            return ErrorCode.Success;
        }

        public ErrorCode UpdateStore(int id, Store store, ref String err)
        {
            return _store.Update(id, store, out err);
        }
        public Store GetByUserId(string userId)
        {
            var _db = new UCGrabEntities();
            return _db.Store.FirstOrDefault(store => store.user_id == userId);
        }
        public Store CreateOrRetrieve(String username, ref String err)
        {
            var user = _userMgr.GetUserByUsername(username);
            var userinf = _userMgr.GetUserInfoByUsername(username);
            if (user == null)
            {
                err = "User not found.";
                return null;
            }

            // Retrieve the store using the GetByUserId method from StoreManager
            var store = GetByUserId(user.user_id);

            if (store != null)
            {
                // If the store is found and the store_id is null in user_information, update it
                if (userinf.store_id == null)
                {
                    userinf.store_id = store.id;
                    _userInfo.Update(userinf.id, userinf, out err);
                }
                return store;
            }

            err = "No store found for the user.";
            return null;
        }

        public ErrorCode CreateAccountAndStore(User_Accounts ua, Store store, ref string errorMessage)
        {
            using (var _db = new UCGrabEntities())
            {
                try
                {
                    ua.user_id = Utilities.gUid;
                    ua.verify_code = Utilities.code.ToString();
                    ua.date_created = DateTime.Now;
                    ua.status = (Int32)Status.InActive;
                    
                    if (_db.User_Accounts.Any(u => u.username == ua.username || u.email == ua.email))
                    {
                        errorMessage = "Username or email already exists.";
                        return ErrorCode.Error;
                    }
                    
                    if (_db.Store.Any(s => s.store_name == store.store_name))
                    {
                        errorMessage = "Store name already exists.";
                        return ErrorCode.Error;
                    }
                    
                    _db.User_Accounts.Add(ua);
                    _db.SaveChanges();
                    
                    store.user_id = ua.user_id;
                    store.store_id = Utilities.gUid;
                    store.status = (int)StoreStatus.Inactive;

                    _db.Store.Add(store);
                    _db.SaveChanges();
                    
                    return ErrorCode.Success;
                }
                catch (Exception ex)
                {
                    errorMessage = "An error occurred: " + ex.Message;
                    return ErrorCode.Error;
                }
            }
        }

        public ErrorCode DeleteStore(int storeId, ref string errorMessage)
        {
            var _db = new UCGrabEntities();
            try
            {
                var store = _db.Store.FirstOrDefault(s => s.id == storeId);
                if (store == null)
                {
                    errorMessage = "Store not found.";
                    return ErrorCode.Error;
                }

                _db.Store.Remove(store);
                _db.SaveChanges(); 

                return ErrorCode.Success;
            }
            catch (Exception ex)
            {
                errorMessage = $"An error occurred while deleting the store: {ex.Message}";
                return ErrorCode.Error;
            }
        }


    }
}