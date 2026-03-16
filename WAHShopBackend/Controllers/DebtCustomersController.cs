using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebtCustomersController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        [HttpGet("getDebtByCustomerId/{customerId}")]
        public async Task<IActionResult> GetDebtByCustomerId(int customerId)
        {
            try
            {
                var debtRecord = await _context.DebtCustomers
                    .FirstOrDefaultAsync(dc => dc.CustomerId == customerId);
                if (debtRecord != null)
                {
                    return Ok(debtRecord);
                }
                else
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Keine Schuldenaufzeichnung für den angegebenen Kunden gefunden." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
        [HttpDelete("deleteDebtByCustomerId/{customerId}")]
        public async Task<IActionResult> DeleteDebtByCustomerId(int customerId)
        {
            try
            {
                var debtRecord = await _context.DebtCustomers
                    .FirstOrDefaultAsync(dc => dc.CustomerId == customerId);
                if (debtRecord != null)
                {
                    _context.DebtCustomers.Remove(debtRecord);
                    await _context.SaveChangesAsync();
                    return Ok(new ValidationResult { Result = true, Message = "Schuldenaufzeichnung erfolgreich gelöscht." });
                }
                else
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Keine Schuldenaufzeichnung für den angegebenen Kunden gefunden." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = true, Message = ex.Message });
            }
        }
    }
}
