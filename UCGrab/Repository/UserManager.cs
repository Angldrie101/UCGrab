using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;
using UCGrab.Utils;

namespace UCGrab.Repository
{
    public class UserManager
    {
        private BaseRepository<User_Accounts> _userAcc;
        private BaseRepository<User_Information> _userInfo;
        private BaseRepository<Store> _store;
        private UCGrabEntities _dbContext;
        public UserManager()
        {
            _userAcc = new BaseRepository<User_Accounts>();
            _userInfo = new BaseRepository<User_Information>();
            _store = new BaseRepository<Store>();
            _dbContext = new UCGrabEntities();
        }

        public User_Accounts GetUserById(int Id)
        {
            return _userAcc.Get(Id);
        }
        public User_Accounts GetUserByUserId(String userId)
        {
            return _userAcc._table.Where(m => m.user_id == userId).FirstOrDefault();
        }
        public User_Accounts GetUserByUsername(String username)
        {
            return _userAcc._table.Where(m => m.username == username).FirstOrDefault();
        }
        public User_Accounts GetUserByEmail(String email)
        {
            return _userAcc._table.Where(m => m.email == email).FirstOrDefault();
        }
        public User_Accounts RetrieveData(int id, ref String err)
        {
            var user = GetUserById(id);
            return GetUserByUserId(user.user_id);
        }
        public ErrorCode SignIn(String username, String password, ref String errMsg)
        {
            var userSignIn = GetUserByUsername(username);
            if (userSignIn == null)
            {
                errMsg = "Username not exist! Try again.";
                return ErrorCode.Error;
            }

            if (!userSignIn.password.Equals(password))
            {
                errMsg = "Password is Incorrect";
                return ErrorCode.Error;
            }

            // user exist
            errMsg = "Login Successful";
            return ErrorCode.Success;
        }

        public ErrorCode SignUp(User_Accounts ua, ref String errMsg)
        {
            ua.user_id = Utilities.gUid;
            ua.verify_code = Utilities.code.ToString();
            ua.date_created = DateTime.Now;
            ua.status = (Int32)Status.InActive;

            if (GetUserByUsername(ua.username) != null)
            {
                errMsg = "Username Already Exist";
                return ErrorCode.Error;
            }

            if (GetUserByEmail(ua.email) != null)
            {
                errMsg = "Email Already Exist";
                return ErrorCode.Error;
            }

            if (_userAcc.Create(ua, out errMsg) != ErrorCode.Success)
            {
                return ErrorCode.Error;
            }

            // use the generated code for OTP "ua.code"
            // send email or sms here...........

            return ErrorCode.Success;
        }

        public ErrorCode UpdateUser(User_Accounts ua, ref String errMsg)
        {
            return _userAcc.Update(ua.id, ua, out errMsg);
        }
        public ErrorCode UpdateUserInformation(User_Information ua, ref String errMsg)
        {
            return _userInfo.Update(ua.id, ua, out errMsg);
        }
        public User_Information GetUserInfoById(int id)
        {
            return _userInfo.Get(id);
        }
        public User_Information GetUserInfoByUsername(String username)
        {
            var userAcc = GetUserByUsername(username);
            return _userInfo._table.Where(m => m.user_id == userAcc.user_id).FirstOrDefault();
        }
        public User_Information GetUserInfoByUserId(String userId)
        {
            return _userInfo._table.Where(m => m.user_id == userId).FirstOrDefault();
        }

        public User_Information GetUserInfoByStoreId(int storeId)
        {
            return _userInfo._table.Where(m => m.store_id == storeId).FirstOrDefault();
        }
        public User_Information CreateOrRetrieve(String username, ref String err)
        {
            var User = GetUserByUsername(username);
            var UserInformation = GetUserInfoByUserId(User.user_id);

            if (UserInformation != null)
                return UserInformation;

            UserInformation = new User_Information();
            UserInformation.user_id = User.user_id;
            UserInformation.email = User.email;
            UserInformation.status = (Int32)Status.Active;

            var userEmail = User.email;
            if (userEmail != null)
            {
                UserInformation.email = userEmail;
            }

            _userInfo.Create(UserInformation, out err);

            return GetUserInfoByUserId(User.user_id);

        }
        public User_Information Retrieve(String username, ref String err)
        {
            // Retrieve the user by username
            var User = GetUserByUsername(username);
            if (User == null)
            {
                err = "User not found";
                return null;
            }

            // Retrieve the user information by user_id
            var UserInformation = GetUserInfoByUserId(User.user_id);

            // If user information already exists, return it
            if (UserInformation != null)
                return UserInformation;

            // Create new user information if it does not exist
            UserInformation = new User_Information
            {
                user_id = User.user_id,
                email = User.email,
                status = (Int32)Status.Active
            };

            // Ensure email is set correctly
            var userEmail = User.email;
            if (!String.IsNullOrEmpty(userEmail))
            {
                UserInformation.email = userEmail;
            }

            // Create the new user information in the database
            var errorCode = _userInfo.Create(UserInformation, out err);
            if (errorCode != ErrorCode.Success)
            {
                // Handle the error if creation failed
                return null;
            }
            // Retrieve the user information after creation
            return GetUserInfoByUserId(User.user_id);
        }

        public List<User_Accounts> GetAllBUserInfo()
        {
            return _userAcc.GetAll().ToList();
        }

      
    }
}