using System;
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
        BaseRepository<Favorites> _fav;
        StoreManager _storeMgr;
        public OrderManager()
        {
            _db = new UCGrabEntities();
            _order = new BaseRepository<Order>();
            _userMgr = new UserManager();
            _productMgr = new ProductManager();
            _orderDetail = new BaseRepository<Order_Detail>();
            _fav = new BaseRepository<Favorites>();
            _storeMgr = new StoreManager();
        }

        public Order GetOrCreateOrderByUserId(String userId, Product prod, ref String err)
        {
            String orderNo = String.Empty;

            var user = _userMgr.GetUserInfoByUserId(prod.user_id);
            var store = _storeMgr.GetStoreByUserId(prod.user_id);


            var order = _order._table.Where(m => m.user_id == userId && m.store_id == store.id && m.order_status == (Int32)OrderStatus.Open).FirstOrDefault();


            if (order != null)
            {
                return order;
            }
            else if (order == null || order.order_status != (Int32)OrderStatus.Open)
            {
                order = new Order();
                order.user_id = userId;
                order.order_status = (Int32)OrderStatus.Open;
                order.store_id = user.store_id;
                order.order_date = DateTime.Now;

                _order.Create(order, out err);
                
            }

            return order;
        }

        public ErrorCode AddCart(String userId, int productId, int qty, ref String error)
        {
            var product = _productMgr.GetProductById(productId);
            if (product == null)
            {
                error = "Product not found";
                return ErrorCode.Error;
            }
            var order = GetOrCreateOrderByUserId(userId, product, ref error);
            
            var orderdet = GetOrderDetailByOrderId(order.order_id);

            
                var result = AddUpdateCartQty(new Order_Detail
                {
                    order_id = order.order_id,
                    product_id = productId,
                    quatity = qty,
                    price = product.price
                }, order);

                return result;

            


            //if (result == ErrorCode.Error)
            //{
            //    error = Message;
            //    return ErrorCode.Error;
            //}

            return ErrorCode.Success;
        }

        public ErrorCode AddUpdateCartQty(Order_Detail orderItem, Order order)
        {
            try
            {
                var existingOrderDetail = _order
                    .Get(order.order_id)?
                    .Order_Detail
                    .FirstOrDefault(m => m.product_id == orderItem.product_id);

                if (existingOrderDetail != null)
                {
                    existingOrderDetail.quatity += orderItem.quatity;

                    return _orderDetail.Update(existingOrderDetail.id, existingOrderDetail, out Message);
                }
                
                return _orderDetail.Create(orderItem, out Message);
            }
            catch (Exception ex)
            {
                Message = $"Failed to update cart quantity: {ex.Message}";
                return ErrorCode.Error;
            }
        }

        public ErrorCode AddToFavorites(String userId, int productId, int qty, ref String error)
        {
            var product = _productMgr.GetProductById(productId);
            if (product == null)
            {
                error = "Product not found.";
                return ErrorCode.Error;
            }
            
            var existingFavorite = _db.Favorites
                .FirstOrDefault(f => f.user_id == userId && f.product_id == productId);

            if (existingFavorite != null)
            {
                error = "Product is already in favorites.";
                return ErrorCode.Error;
            }

            // Add to favorites
            var favorite = new Favorites
            {
                user_id = userId,
                product_id = productId,
                quantity = qty
            };

            _db.Favorites.Add(favorite);
            _db.SaveChanges();

            return ErrorCode.Success;
        }
        public ErrorCode AddUpdateFavoritesQty(Favorites favItem, Product product)
        {
            try
            {
                string err = string.Empty;

                // Ensure that the product exists in the database
                var lProduct = _productMgr.GetProductById(favItem.product_id);
                if (lProduct == null)
                {
                    err = "Product not found.";
                    return ErrorCode.Error;
                }
                
                var existingFavorite = _fav._table.Where(m => m.user_id == m.user_id && m.product_id == product.id).FirstOrDefault();

                if (existingFavorite == null)
                {
                    // If not, create a new favorite item
                    return _fav.Create(favItem, out err);
                }
                else
                {
                    // If it exists, update the favorite item (e.g., increase quantity)
                    existingFavorite.quantity += favItem.quantity;  // Update quantity or other fields as necessary

                    return _fav.Update(existingFavorite.id, existingFavorite, out err);
                }
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
                .Include(o => o.Order_Detail.Select(od => od.Product)) // Correct navigation
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
        public Order_Detail GetOrderDetailByOrderId (int orderId)
        {
            return _orderDetail.GetAll().Where(o => o.order_id == orderId).FirstOrDefault();
        }
        public ErrorCode UpdateOrderDetail(int id, Order_Detail orderDt, ref String err)
        {
            return _orderDetail.Update(id, orderDt, out err);
        }

        public ErrorCode DeleteOrderDetail(int id, ref String err)
        {
            return _orderDetail.Delete(id, out err);
        }
        
        public ErrorCode PlaceOrder(string userId, OrderViewModel model, string gcashReceiptFileName, ref string error)
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
                    if (!string.IsNullOrEmpty(gcashReceiptFileName))
                    {
                        order.gcash_receipt = gcashReceiptFileName;
                    }

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

        public Order GetOrCreateOpenOrderForCart(string userId, ref string err)
        {
            // Get user info based on userId
            var user = _userMgr.GetUserInfoByUserId(userId);

            // Retrieve existing open order or create a new one
            var order = _order._table.Where(m => m.user_id == userId && m.store_id == user.store_id && m.order_status == (int)OrderStatus.Open).FirstOrDefault();

            if (order == null)
            {
                // If no open order exists, create a new one
                order = new Order
                {
                    user_id = userId,
                    order_status = (int)OrderStatus.Open,
                    store_id = user.store_id,
                    order_date = DateTime.Now
                };

                // Create the new order in the database
                _order.Create(order, out err);

                // Handle error if any
                if (!string.IsNullOrEmpty(err))
                {
                    throw new Exception("Error creating order: " + err);
                }
            }

            return order;
        }
        public decimal GetTotalByOrderId(int orderId)
        {
            // Fetch all order details for the given orderId
            var orderdetails = _orderDetail._table.Where(o => o.order_id == orderId);

            // Multiply price and quantity for each order detail and sum them up
            var totalOrder = orderdetails.Sum(o => o.price * o.quatity) ?? 0m; // Use 0m if the result is null

            return totalOrder;
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
