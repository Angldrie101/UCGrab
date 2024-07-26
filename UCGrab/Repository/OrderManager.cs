using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using UCGrab.Database;
using UCGrab.Utils;

namespace UCGrab.Repository
{
    public class OrderManager
    {
        public String Message = String.Empty;
        //
        UCGrabEntities _db;
        BaseRepository<Order> _order;
        BaseRepository<Order_Detail> _orderDetail;
        UserManager _userMgr;
        ProductManager _productMgr;
        public OrderManager()
        {
            _db = new UCGrabEntities();
            _order = new BaseRepository<Order>();
            _userMgr = new UserManager();
            _productMgr = new ProductManager();
            _orderDetail = new BaseRepository<Order_Detail>();
        }

        public Order GetOrCreateOrderByUserId(String userId, Product prod, ref String err)
        {
            String orderNo = String.Empty;

            var user = _userMgr.GetUserInfoByUserId(prod.user_id);

            var order = _order._table.Where(m => m.user_id == userId && m.store_id == user.store_id).FirstOrDefault();
            if (order == null || order.order_status != (Int32)OrderStatus.Pending)
            {
                order = new Order();
                order.user_id = userId;
                order.order_status = (Int32)OrderStatus.Pending;
                order.store_id = user.store_id;
                order.order_date = DateTime.Now;

                _order.Create(order, out err);

                return order;
            }

            return order;
        }

        public ErrorCode AddCart(String userId, int productId, int qty, ref String error)
        {
            var product = _productMgr.GetProductById(productId);
            if (product == null)
            {
                error = "Not Found";
                return ErrorCode.Error;
            }

            var order = GetOrCreateOrderByUserId(userId, product, ref error);
            var orDetail = new Order_Detail();
            orDetail.order_id = order.order_id;
            orDetail.product_id = productId;
            orDetail.quatity = qty;
            orDetail.price = product.price;

            if (AddUpdateCartQty(orDetail, order) == ErrorCode.Error)
            {
                error = Message; 
                return ErrorCode.Error;
            }


            return ErrorCode.Success;
        }

        public ErrorCode AddUpdateCartQty(Order_Detail orderItem, Order order)
        {
            try
            {
                String err = String.Empty;
                var lproduct = _productMgr.GetProductById(orderItem.product_id);
                var lOrderItem = _order.Get(order.order_id).Order_Detail.Where(m => m.product_id == orderItem.product_id).FirstOrDefault();
                if (lOrderItem == null)
                {

                    return _orderDetail.Create(orderItem, out Message);
                }
                // retrieve the order detail to update qty
                var orDt = _orderDetail.Get(lOrderItem.id);
                orDt.quatity += orderItem.quatity;

                return _orderDetail.Update(orDt.id, orDt, out Message);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return ErrorCode.Error;
            }

        }

        public List<Order> GetOrderByUserId(String userId)
        {
            return _order._table.Where(m => m.user_id == userId && m.order_status == (Int32)OrderStatus.Pending).ToList();
        }

        public int GetCartCountByUserId(String userId)
        {
            var count = _db.sp_getCartCountByUserId(userId).FirstOrDefault();
            return (Int32)count;
        }
        public Order_Detail GetOrderDetailById(int id)
        {
            return _orderDetail.Get(id);
        }

        public ErrorCode UpdateOrderDetail(int id, Order_Detail orderDt, ref String err)
        {
            return _orderDetail.Update(id, orderDt, out err);
        }

        public ErrorCode DeleteOrderDetail(int id, ref String err)
        {
            return _orderDetail.Delete(id, out err);
        }
    }
}