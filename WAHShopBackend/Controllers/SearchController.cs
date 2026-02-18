using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;

        [HttpGet("searchProducts")]
        public async Task<IActionResult> SearchProducts([FromQuery] string query, [FromQuery] List<int>? excludeIds)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Search query cannot be empty." });
            }
            try
            {
                var results = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.TaxRate)
                    .Include(p => p.ProductGroup)
                    .Include(p => p.ProductImages)
                    .Where(p => p.Quantity > 0 &&
                                !excludeIds!.Contains(p.Id) &&
                                (p.Name_de!.ToLower().Contains(query.ToLower()) ||
                                p.Name_ar!.ToLower().Contains(query.ToLower())))
                    .OrderBy(p => p.Name_de)
                    .Take(11)
                    .ToListAsync();
                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
