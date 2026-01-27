using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        // customers APIs
        [HttpGet("getAllCustomers")]
        public async Task<IActionResult> GetAllCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .Include(c => c.DistributionLine)
                    .ToListAsync();
                if (customers == null || customers.Count == 0)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Keine Kunden gefunden." });
                }
                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPost("addCustomer")]
        public async Task<IActionResult> AddCustomer([FromBody] Customers customer)
        {
            if (customer == null)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Kundendaten sind null" });
            }
            try
            {
                _context.Customers.Add(customer);
                var result = await _context.SaveChangesAsync();
                if (result <= 0)
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Die Erstellung des Kunden ist fehlgeschlagen." });
                return Ok(new ValidationResult { Result = true, Message = $"Kunde erfolgreich erstellt, Id:{customer.Id}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPut("updateCustomer")]
        public async Task<IActionResult> UpdateCustomer([FromBody] Customers customer)
        {
            if (customer == null || customer.Id <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Kundendaten." });
            }
            try
            {
                var existingCustomer = await _context.Customers.FindAsync(customer.Id);
                if (existingCustomer == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Kunde nicht gefunden." });
                }
                _context.Entry(existingCustomer).CurrentValues.SetValues(customer);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, Message = "Kunde erfolgreich aktualisiert." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Die Aktualisierung des Kunden ist fehlgeschlagen." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpDelete("deleteCustomer/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Kunden Id." });
            }
            try
            {
                var existingCustomer = await _context.Customers.FindAsync(id);
                if (existingCustomer == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Kunde nicht gefunden." });
                }
                _context.Customers.Remove(existingCustomer);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, Message = "Kunde erfolgreich gelöscht." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Die Löschung des Kunden ist fehlgeschlagen." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getCustomerById/{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Kunden Id." });
            }
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.DistributionLine)
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (customer == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Kunde nicht gefunden." });
                }
                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
