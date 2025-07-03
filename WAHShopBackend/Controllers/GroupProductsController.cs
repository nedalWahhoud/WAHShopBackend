using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GroupProductsController(MyDbContext context) : ControllerBase
    {

        private readonly MyDbContext _context = context;
        [HttpGet("getProductsByGroupProductsId/{groupProductsId}")]
        public async Task<IActionResult> GetProductsByGroupProductsId(int groupProductsId, [FromQuery] GetItems<Product>? getItems = null, [FromQuery] List<int>? excludeProductsIds = null)
        {
            getItems ??= new();

            excludeProductsIds = excludeProductsIds ?? [];
            if (groupProductsId <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Invalid group products ID." });
            }

            try
            {
                var products = await _context.Products
                    .Where(p => p.ProductGroupID == groupProductsId && !excludeProductsIds.Contains(p.Id) &&
                    p.Quantity > 0)
                    .ToListAsync();
                if (products == null || !products.Any())
                {
                    return NotFound(new ValidationResult { Result = false, Message = "No products found for the specified group." });
                }

                getItems.Items = products!;


                return Ok(getItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getAllGroupProducts")]
        public async Task<IActionResult> GetAllGroupProducts()
        {
            try
            {
                var groupProducts = await _context.GroupProducts.ToListAsync();
                if (groupProducts == null || groupProducts.Count == 0)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "No group products found." });
                }
                return Ok(groupProducts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
