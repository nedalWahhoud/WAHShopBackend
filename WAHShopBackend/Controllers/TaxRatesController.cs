using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaxRatesController(MyDbContext context) : ControllerBase
    {
        public readonly MyDbContext _context = context;

        [HttpGet("getTaxRates")]
        public async Task<IActionResult> GetTaxRates()
        {
            try
            {
                var taxRates = await _context.TaxRates
                    .AsNoTracking()
                    .ToListAsync();

                var getItems = new GetItems<TaxRate>
                {
                    Items = taxRates,
                    AllItemsLoaded = true
                };

                return Ok(getItems);
            }
            catch
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error" });
            }
        }

        [HttpGet("getTaxRateById/{id}")]
        public async Task<IActionResult> GetTaxRateById(int id)
        {
            if (id <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Steuer-Id" });
            try
            {
                var taxRate = await _context.TaxRates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);
                if (taxRate == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Steuersatz nicht gefunden." });
                return Ok(taxRate);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
