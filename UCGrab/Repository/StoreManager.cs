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
        public Store GetStoreById(int? id)
        {
            return _store.Get(id);
        }
        public Store GetStoreByGuId(String id)
        {
            return _store.GetAll().Where(m => m.store_id == id).FirstOrDefault();
        }

        public ErrorCode UpdateStore(int id, Store store, ref String err)
        {
            return _store.Update(id, store, out err);
        }
        public Store CreateOrRetrieve(String username, ref String err)
        {
            var user = _userMgr.GetUserByUsername(username);
            var userInf = _userMgr.GetUserInfoByUserId(user.user_id);

            if (userInf.store_id != null)
                return _store.Get(userInf.store_id);

            var store = new Store();
            store.store_id = Utilities.gUid;
            store.store_name = $"{user.username}.Store";

            if (_store.Create(store, out err) != ErrorCode.Success)
            {
                // Return Error
                return null;
            }
            store = GetStoreByGuId(store.store_id);
            // Update user information assign store id
            userInf.id = store.id;
            //
            _userInfo.Update(userInf.id, userInf, out err);

            return store;
        }
    }
}