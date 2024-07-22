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

        public Store GetStoreByUserId(string userId)
        {

            return _store._table.FirstOrDefault(m => m.user_id == userId);
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

        public ErrorCode StoreUpdate(Store st, ref string errMsg)
        {
            return _store.Update(st.id, st, out errMsg);
        }
       
    }
}