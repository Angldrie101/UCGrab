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
            if (order == null || order.order_status != (Int32)OrderStatus.Open)
            {
                order = new Order();
                order.user_id = userId;
                order.order_status = (Int32)OrderStatus.Open;
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
            return _order._table.Where(m => m.user_id == userId && m.order_status == (Int32)OrderStatus.Open).ToList();
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

        public ErrorCode PlaceOrder(string userId, Order model, ref string error)
        {
            try
            {
                // Get the open order for the user
                var order = _order._table.FirstOrDefault(m => m.user_id == userId && m.order_status == (int)OrderStatus.Open);

                if (order != null)
                {
                    // Update the order details
                    order.order_status = (int)OrderStatus.Pending;
                    order.payment_method = model.payment_method;
                    order.order_date = DateTime.Now;
                    // Update additional fields from the model
                    order.checkOut_option = model.checkOut_option;
                    order.shipping_address = model.shipping_address;
                    order.building = model.building;
                    order.room = model.room;
                    order.firstname = model.firstname;
                    order.lastname = model.lastname;
                    order.phone = order.phone;
                    order.email = order.email;
                    order.additional_info = model.additional_info;
                    

                    // Save changes
                    _order.Update(order.order_id, order, out error);

                    return ErrorCode.Success;
                }
                else
                {
                    error = "No open order found for the user.";
                    return ErrorCode.Error;
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return ErrorCode.Error;
            }
        }

    }

}
