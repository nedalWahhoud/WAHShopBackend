using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;

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
                    return NotFound(new { Message = "Keine Schuldenaufzeichnung für den angegebenen Kunden gefunden." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }
    }
}
