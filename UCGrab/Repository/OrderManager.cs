﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using UCGrab.Database;
using UCGrab.Models;
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

        public List<Order> GetAllOrders()
        {
            return _order._table.Where(m => m.order_status == (Int32)OrderStatus.Confirmed && m.checkOut_option == (Int32)CheckoutOption.Deliver).ToList();
        }

        public List<Order> GetAllOrderToDeliver(String userId)
        {
            return _order._table.Where(m => m.user_id == userId && m.order_status == (Int32)OrderStatus.ReadyToDeliver).ToList();
        }
        
        public Order GetOrderbyId(int id)
        {
            return _db.Order
              .Include(o => o.Products)
              .FirstOrDefault(o => o.order_id == id);
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

        public ErrorCode PlaceOrder(string userId, OrderViewModel model, ref string error)
        {
            try
            {
                var order = _order._table.FirstOrDefault(m => m.user_id == userId && m.order_status == (int)OrderStatus.Open);
                var orderDetails = _orderDetail._table.Where(od => od.order_id == order.order_id).ToList();

                if (order != null)
                {
                    order.order_status = (int)OrderStatus.Pending;
                    order.payment_method = model.PaymentMethod;
                    order.order_date = DateTime.Now;
                    order.checkOut_option = model.CheckOutOption;
                    order.building = model.Building;
                    order.room = model.Room;
                    order.firstname = model.Firstname;
                    order.lastname = model.Lastname;
                    order.phone = model.Phone;
                    order.email = model.Email;
                    order.additional_info = model.AdditionalInfo;
                    
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

        public List<Order_Detail> GetOrderDetailsByOrderId(int orderId)
        {
            return _orderDetail._table.Where(od => od.order_id == orderId).ToList();
        }

        public List<Order> GetUserOrderByUserId(String userId)
        {
            return _order._table.Where(m => m.user_id == userId).ToList();
        }

        public ErrorCode CancelOrder(int orderId, string userId)
        {
            try
            {
                var order = _order.Get(orderId);
                if (order != null && order.user_id == userId && order.order_status == (int)OrderStatus.Pending)
                {
                    order.order_status = (int)OrderStatus.Cancelled;
                    string error;
                    _order.Update(order.order_id, order, out error);
                    return ErrorCode.Success;
                }
                else
                {
                    return ErrorCode.Error;
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return ErrorCode.Error;
            }
        }


        public int GetTotalOrders(string userId)
        {
            return _order._table.Count(o => o.user_id == userId && o.order_status != (int)OrderStatus.Pending);
        }

        public List<Order> GetOrdersByStoreId(int storeId)
        {
            var orders = _db.Order
           .Where(o => o.store_id == storeId)
           .Include(o => o.Order_Detail.Select(od => od.Product))
           .ToList();

            System.Diagnostics.Debug.WriteLine($"Filtered Orders: {string.Join(", ", orders.Select(o => o.order_id))}");
            return orders;
        }

        public List<Order> GetOrdersByDeliveryId(string deliveryId)
        {
            var orders = _db.Order
           .Where(o => o.delivery_id == deliveryId)
           .Include(o => o.Order_Detail.Select(od => od.Product))
           .ToList();

            return orders;
        }

        public ErrorCode ConfirmOrder(int orderId)
        {
            try
            {
                var order = _order.Get(orderId);
                if (order != null && order.order_status == (int)OrderStatus.Pending)
                {
                    order.order_status = (int)OrderStatus.Confirmed;
                    string error;
                    _order.Update(order.order_id, order, out error);
                    return ErrorCode.Success;
                }
                else
                {
                    return ErrorCode.Error;
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return ErrorCode.Error;
            }
        }

        public Order GetOpenOrderByUserId(string userId)
        {
            return _db.Order.FirstOrDefault(o => o.user_id == userId && o.order_status == (int)OrderStatus.Open);
        }

        public Order_Detail GetOrderDetailByOrderIdAndProductId(int orderId, int productId)
        {
            return _db.Order_Detail.FirstOrDefault(od => od.order_id == orderId && od.product_id == productId);
        }

        public void UpdateCart(int orderDetailId, Order_Detail updatedOrderDetail, ref string errorMessage)
        {
            try
            {
                var orderDetail = _db.Order_Detail.Find(orderDetailId);
                if (orderDetail != null)
                {
                    orderDetail.quatity = updatedOrderDetail.quatity;
                    _db.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
        }

        public void AddOrderDetail(Order_Detail newOrderDetail, ref string errorMessage)
        {
            try
            {
                _db.Order_Detail.Add(newOrderDetail);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }
        }

        public decimal GetProductPrice(int productId)
        {
            return _db.Product.Where(p => p.id == productId).Select(p => p.price).FirstOrDefault();
        }


        public ErrorCode ToDeliverOrder(int orderId)
        {
            try
            {
                var order = _order.Get(orderId);
                if (order != null && order.order_status == (int)OrderStatus.ReadyToDeliver)
                {
                    order.order_status = (int)OrderStatus.Delivered;
                    string error;
                    _order.Update(order.order_id, order, out error);
                    return ErrorCode.Success;
                }
                else
                {
                    return ErrorCode.Error;
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return ErrorCode.Error;
            }
        }

        public ErrorCode ReadyToDeliver(int orderId)
        {
            try
            {
                var order = _order.Get(orderId);
                if (order != null && order.order_status == (int)OrderStatus.Confirmed)
                {
                    order.order_status = (int)OrderStatus.ReadyToDeliver;
                    string error;
                    _order.Update(order.order_id, order, out error);
                    return ErrorCode.Success;
                }
                else
                {
                    return ErrorCode.Error;
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return ErrorCode.Error;
            }
        }

    }

}
