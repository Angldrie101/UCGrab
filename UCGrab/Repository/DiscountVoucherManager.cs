using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;
using UCGrab.Utils;

namespace UCGrab.Repository
{
    public class DiscountVoucherManager
    {
        UCGrabEntities _db;
        BaseRepository<Discounts> _discount;
        BaseRepository<Vouchers> _vouchers;

        public DiscountVoucherManager()
        {
            _db = new UCGrabEntities();
            _discount = new BaseRepository<Discounts>();
            _vouchers = new BaseRepository<Vouchers>();
        }

        public ErrorCode CreateDiscount(Discounts dis, ref String err)
        {
            return _discount.Create(dis, out err);
        }

        public List<Discounts> ListDiscountsByStoreId(int storeId)
        {
            var discounts = _db.Discounts.Where(d => d.store_id == storeId).ToList();
            return discounts;
        }
        public List<Vouchers> ListVoucherByStoreId(int storeId)
        {
            return _db.Vouchers.Where(d => d.store_id == storeId).ToList();
        }

        public ErrorCode AddDiscount(Discounts discount, ref string errorMessage)
        {
            try
            {
                _db.Discounts.Add(discount);
                _db.SaveChanges();
                return ErrorCode.Success; // Discount added successfully
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return ErrorCode.Error; // Failed to add discount
            }
        }
        public ErrorCode AddVoucher(Vouchers vouchers, ref string errorMessage)
        {
            try
            {
                _db.Vouchers.Add(vouchers);
                _db.SaveChanges();
                return ErrorCode.Success; // Discount added successfully
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return ErrorCode.Error; // Failed to add discount
            }
        }
    }
}