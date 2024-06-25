using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using UCGrab.Database;
using UCGrab.Repository;

namespace UCGrab.Utils
{
    public class MyRole : RoleProvider
    {
        public BaseRepository<User_Role> _role = new BaseRepository<User_Role>();
        public BaseRepository<User_Accounts> _userAcc = new BaseRepository<User_Accounts>();
        public override string ApplicationName
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            return _role.GetAll().Select(m => m.rolename).ToArray();
        }

        public override string[] GetRolesForUser(string username)
        {
            UCGrabEntities db = new UCGrabEntities();
            return db.vw_Role.Where(m => m.username == username).Select(m => m.rolename).ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            throw new NotImplementedException();
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            throw new NotImplementedException();
        }
    }
}