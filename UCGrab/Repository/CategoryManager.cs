using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;
using UCGrab.Utils;

namespace UCGrab.Repository
{
    public class CategoryManager
    {
        private BaseRepository<Category> _category;
        private UserManager _userMgr;
        public CategoryManager()
        {
            _category = new BaseRepository<Category>();
            _userMgr = new UserManager();
        }

        public Category GetCategoryById(int? id)
        {
            return _category.Get(id);
        }

        public List<Category> ListCategory(String username)
        {
            var user = _userMgr.GetUserByUsername(username);
            return _category._table.Where(m => m.user_id == user.user_id).ToList();
        }

        public ErrorCode CreateCategory(Category category, ref String err)
        {
            return _category.Create(category, out err);
        }
        public ErrorCode UpdateCategory(Category category, ref String err)
        {
            return _category.Update(category.category_id, category, out err);
        }
        public ErrorCode DeleteCategory(int? id, ref String err)
        {
            return _category.Delete(id, out err);
        }
    }
}