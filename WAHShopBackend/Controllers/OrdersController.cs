using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.EmailF;
using WAHShopBackend.Models;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController(MyDbContext context, EmailService emailService) : ControllerBase
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

                        // send order confirmation email
                        if (order.User != null && !string.IsNullOrWhiteSpace(order.User.Email))
                        {
                            // get seved order with all related data
                            var savedOrder = await _context.Orders
                                           .Include(o => o.DiscountCode)
                                           .Include(o => o.DiscountCategory)
                                           .Include(o => o.Address)
                                           .Include(o => o.PaymentMethod).ThenInclude(pm => pm!.BankTransferDetails)
                                           .Include(o => o.Status)
                                           .Include(o => o.ShippingProviders)
                                           .Include(o => o.OrderItems)
                                           .ThenInclude(oi => oi.Product)
                                           .FirstOrDefaultAsync(o => o.Id == order.Id);

                            if (savedOrder != null)
                            {
                                _ = _emailService.OrderConfirmation(savedOrder);
                            }
                        }
                        // check if discount code is set, if so, update the discount code usage
                        if (order.DiscountCodeId.HasValue)
                        {
                            var discountCode = await _context.DiscountCodes.FindAsync(order.DiscountCodeId.Value);
                            if (discountCode != null)
                            {
                                discountCode.TimesUsed++;
                                _context.DiscountCodes.Update(discountCode);
                                await _context.SaveChangesAsync();
                            }
                        }
                        // check if discount category is set, if so, update the discount category usage
                        if (order.DiscountCategoryId.HasValue)
                        {
                            var discountCategory = await _context.DiscountCategory.FindAsync(order.DiscountCategoryId.Value);
                            if (discountCategory != null)
                            {
                                discountCategory.TimesUsed++;
                                _context.DiscountCategory.Update(discountCategory);
                                await _context.SaveChangesAsync();
                            }
                        }
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
            if (getItems.AllItemsLoaded)
                return Ok(getItems);
            try
            {
                var orders = await _context.Orders
                   .AsNoTracking()
                   .Where(o => o.UserId == userId)
                   .OrderByDescending(o => o.Id)
                   .Skip(getItems.CurrentPage * getItems.PageSize)
                   .Take(getItems.PageSize)
                   .Include(o => o.PaymentMethod)
                   .Include(o => o.Status)
                   .Include(o => o.Address)
                   .Include(o => o.DiscountCode)
                   .Include(o => o.DiscountCategory).ThenInclude(o => o.Category)
                   .Include(o => o.ShippingProviders)
                   .ToListAsync();

                var orderIds = orders.Select(o => o.Id).ToList();
                // Die Aufteilung des Datenabrufs auf mehrere Schritte führt aufgrund der Komplexität und der großen Datenmenge zu extremer Langsamkeit
                // , daher nehmen wir die Bestellelemente mit den Produkten einzeln in Angriff.
                var orderItems = await _context.OrderItems
                    .Where(oi => orderIds.Contains(oi.OrderId))
                    .Include(oi => oi.Product)
                    .ToListAsync();
                // Dann führen wir es mit den Anfragen zusammen und beschleunigen so den Prozess des Abrufens der Anfragedaten.
                foreach (var order in orders)
                {
                    order.OrderItems = orderItems.Where(oi => oi.OrderId == order.Id).ToList();
                }

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
        [HttpGet("getOrderscustomer/{userId}")]
        public async Task<IActionResult> GetOrders1(int userId, [FromQuery] GetItems<Order> getItems)
        {
            if (getItems.AllItemsLoaded)
                return Ok(getItems);
            try
            {
                var query = _context.Orders
                    .Include(o => o.PaymentMethod)
                    .Include(o => o.Status)
                    .Include(o => o.Address)
                    .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                    .Include(o => o.DiscountCode)
                    .Include(o => o.DiscountCategory)
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
                      .AsNoTracking()
                      .Include(o => o.PaymentMethod)
                      .Include(o => o.Status)
                      .Include(o => o.Address)
                      .Include(o => o.User)
                      .Include(o => o.DiscountCode)
                      .Include(o => o.DiscountCategory)
                      .Include(o => o.ShippingProviders)
                      .Where(o =>
                      (!statusFilter.HasValue || o.StatusId == statusFilter.Value) &&
                      (excludeIds == null || !excludeIds.Contains(o.Id)));
                   //
                   var orders = await query
                       .OrderByDescending(o => o.OrderDate)
                       .Take(getItems.PageSize)
                       .ToListAsync();


                var orderIds = orders.Select(o => o.Id).ToList();
                // Die Aufteilung des Datenabrufs auf mehrere Schritte führt aufgrund der Komplexität und der großen Datenmenge zu extremer Langsamkeit
                // , daher nehmen wir die Bestellelemente mit den Produkten einzeln in Angriff.
                var orderItems = await _context.OrderItems
                    .Where(oi => orderIds.Contains(oi.OrderId))
                    .Include(oi => oi.Product).ThenInclude(p => p!.TaxRate)
                    .Include(oi => oi.Product!.Category)
                    .ToListAsync();
                // Dann führen wir es mit den Anfragen zusammen und beschleunigen so den Prozess des Abrufens der Anfragedaten.
                foreach (var order in orders)
                {
                    order.OrderItems = orderItems.Where(oi => oi.OrderId == order.Id).ToList();
                }

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
        [HttpGet("getOrderCountByStatusId")]
        public async Task<IActionResult> GetOrderCountByStatusId([FromQuery] List<int> statusIds)
        {
            try
            {
                var count = await _context.Orders
                    .Where(o => statusIds.Contains(o.StatusId))
                    .CountAsync();
                return Ok(new OrdersCount { Count = count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while retrieving the order count: {ex.Message}" });
            }
        }
        [HttpPut("updateStatusOrder/{orderId}")]
        public async Task<IActionResult> UpdateStatusOrder(int orderId, [FromBody] int newStatusId)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == orderId);
                if (order == null)
                {
                    return NotFound(new ValidationResult() { Result = false, Message = "Order not found." });
                }
                // update order status
                order.StatusId = newStatusId;
                _context.Orders.Update(order);
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    // reload the order to get the updated status
                    await _context.Entry(order).Reference(o => o.Status).LoadAsync();
                    // send email notification if user email is set
                    if (order.User != null && !string.IsNullOrWhiteSpace(order.User.Email))
                    {
                        _ = _emailService.OrderStatusChanged(order);
                    }
                }
                return Ok(new ValidationResult { Result = result > 0, Message = $"Order status updated successfully for Order ID: {orderId}" });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while updating the order status: {ex.Message}" });
            }
        }
        [HttpGet("getShippingProvider")]
        public async Task<IActionResult> GetShippingProvider()
        {
            try
            {
                var shippingProviders = await _context.ShippingProviders.ToListAsync();
                if (shippingProviders == null || shippingProviders.Count == 0)
                    return NotFound(new ValidationResult() { Result = false, Message = "No shipping providers found." });
                return Ok(shippingProviders);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"An error occurred while retrieving the shipping providers: {ex.Message}" });
            }
        }
        [HttpPut("addOrUpdateTrackingNumber/{orderId}")]
        public async Task<IActionResult> AddOrUpdateTrackingNumber(int orderId, [FromBody] string trackingNumber)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                {
                    return NotFound(new ValidationResult() { Result = false, Message = "Order nicht gefunden." });
                }
                // update tracking number
                order.TrackingNumber = trackingNumber;
                _context.Orders.Update(order);
                var result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Die Trackingnummer wurde erfolgreich aktualisiert: {orderId}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"Beim Aktualisieren der Trackingnummer ist ein Fehler aufgetreten: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }
    }
}
