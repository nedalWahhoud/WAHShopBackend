using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using WAHShopBackend.Data;
using WAHShopBackend.EmailF;
using WAHShopBackend.Models;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController(MyDbContext context,EmailService emailService) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        private readonly EmailService _emailService = emailService;
        [HttpPost("addOrder")]
        public async Task<IActionResult> AddOrder([FromBody] Order order)
        {

            if (order == null || order.OrderItems == null || order.OrderItems.Count == 0)
            {
                return BadRequest(new ValidationResult() { Result = false, Message = "Invalid order data." });
            }
            try
            {
                // update quantity of products in order items
                string quantityMessage = null!;
                foreach (var item in order.OrderItems)
                {
                    var product = await _context.Products.FindAsync(item.ProductId);
                    if (product == null)
                    {
                        return BadRequest(new ValidationResult() { Result = false, Message = $"Product with ID {item.ProductId} not found." });
                    }

                    if (product.Quantity < item.Quantity)
                    {
                        quantityMessage += quantityMessage == null ? quantityMessage! : "\n";

                        quantityMessage += $"Menge von {product.Name_de} nicht verfügbar nur noch {product.Quantity} Stück verfügbar";
                    }
                    else
                        product.Quantity -= item.Quantity;
                }
                if (quantityMessage != null)
                    return BadRequest(new ValidationResult() { Result = false, Message = quantityMessage });


                // add order
                _context.Orders.Add(order);
                var result = await _context.SaveChangesAsync();

                if (result > 0)
                {
                    // check if user email is set, if not get user from database
                    if (order.User == null || string.IsNullOrWhiteSpace(order.User.Email))
                    {
                        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == order.UserId);

                        order.User = user;
                    }
                    if (order.User != null && !string.IsNullOrWhiteSpace(order.User.Email))
                    {
                        _ = _emailService.OrderConfirmation(order);
                    }
                }

                return Ok(new ValidationResult { Result = result > 0, Message = $"Id:{order.Id}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while adding the order: {ex.Message}" });
            }
        }
        [HttpGet("getOrders/{userId}")]
        public async Task<IActionResult> GetOrders(int userId, [FromQuery] GetItems<Order> getItems)
        {
            if(getItems.AllItemsLoaded)
                return Ok(getItems);
            try
            {
                var query = _context.Orders
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.Status)
                    .Include(o => o.Address)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == userId);

                var orders = await query
                    .OrderByDescending(o => o.Id)
                    .Skip(getItems.CurrentPage * getItems.PageSize)
                    .Take(getItems.PageSize)
                    .ToListAsync();

                int allCount = await _context.Orders.Where(o => o.UserId == userId)
                  .CountAsync();

                if (orders.Count == 0 && (getItems.CurrentPage * getItems.PageSize) >= allCount)
                {
                    getItems.AllItemsLoaded = true;
                }

                getItems.Items = orders;

                return Ok(getItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while retrieving the orders: {ex.Message}" });
            }
        }
        [HttpGet("getAllOrderByStatusId/{statusId}")]
        public async Task<IActionResult> GetAllOrderByStatusId(string statusId, [FromQuery] GetItems<Order> getItems, [FromQuery] List<int>? excludeIds)
        {
            try
            {
                excludeIds ??= [];

                int? statusFilter = string.IsNullOrEmpty(statusId) || statusId == "0" ? null : int.Parse(statusId);

                var query = _context.Orders
                   .Include(o => o.PaymentMethod)
                   .Include(o => o.Status)
                   .Include(o => o.Address)
                   .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                   .Include(o => o.User)
                   .Where(o =>
                   (!statusFilter.HasValue || o.StatusId == statusFilter.Value) &&
                   (excludeIds == null || !excludeIds.Contains(o.Id)));
                //
                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Take(getItems.PageSize)
                    .ToListAsync();
                if (orders == null || orders.Count == 0)
                {
                    int allCount = await _context.Orders
                        .Where(o => (!statusFilter.HasValue || o.StatusId == statusFilter.Value))
                        .CountAsync();
                    if (excludeIds.Count >= allCount)
                    {
                        getItems.AllItemsLoaded = true;
                    }
                }

                getItems.Items = orders!;
                return Ok(getItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while retrieving the orders: {ex.Message}" });
            }
        }
        [HttpGet("getOrderStatusList")]
        public async Task<IActionResult> GetOrderStatusList()
        {
            try
            {
                var orderStatuses = await _context.OrderStatus.ToListAsync();
                if (orderStatuses == null || orderStatuses.Count == 0)
                    return NotFound(new ValidationResult() { Result = false, Message = "No order statuses found." });
                return Ok(orderStatuses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while retrieving the order statuses: {ex.Message}" });
            }
        }
        [HttpGet("getPaymentMethods")]
        public async Task<IActionResult> GetPaymentMethods()
        {
            try
            {
                var paymentMethods = await _context.PaymentMethods.ToListAsync();
                if (paymentMethods == null || paymentMethods.Count == 0)
                    return NotFound(new ValidationResult() { Result = false, Message = "No payment methods found." });

                return Ok(paymentMethods);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while retrieving the payment methods: {ex.Message}" });
            }
        }
        [HttpGet("getOrderCountByStatusId{statusId}")]
        public async Task<IActionResult> GetOrderCountByStatusId(int statusId)
        {
            try
            {
                var count = await _context.Orders
                    .Where(o => o.StatusId == statusId)
                    .CountAsync();
                return Ok(new OrdersCount { Count = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while retrieving the order count: {ex.Message}" });
            }
        }

        [HttpPut("updateOrder")]
        public async Task<IActionResult> UpdateOrder([FromBody] Order order)
        {
            if (order == null || order.Id <= 0)
            {
                return BadRequest(new ValidationResult() { Result = false, Message = "Invalid order data." });
            }
            try
            {
                var existingOrder = await _context.Orders.FindAsync(order.Id);
                if (existingOrder == null)
                {
                    return NotFound(new ValidationResult() { Result = false, Message = "Order not found." });
                }

                // update order
                existingOrder.StatusId = order.StatusId;
                existingOrder.Notes = order.Notes;
                existingOrder.TotalPrice = order.TotalPrice;
                existingOrder.PaymentMethodId = order.PaymentMethodId;
                existingOrder.DeliveryAddressId = order.DeliveryAddressId;
                existingOrder.OrderDate = order.OrderDate;
                existingOrder.UserId = order.UserId;

                _context.Orders.Update(existingOrder);
                var result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Order status updated successfully for Order ID: {order.Id}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while updating the order status: {ex.Message}" });
            }
        }
    }
}
