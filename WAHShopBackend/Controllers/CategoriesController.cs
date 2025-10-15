using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        [HttpGet("getCategories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories.ToListAsync();

                var getItems = new GetItems<Categories>
                {
                    Items = categories,
                    AllItemsLoaded = true
                };

                return Ok(getItems);
            }
            catch
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error" });
            }
        }
        [HttpGet("getCategoryById/{categoryId}")]
        public async Task<IActionResult> GetCategoryById(int categoryId)
        {
            try
            {
                var category = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Id == categoryId);
                if (category == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Category not found" });
                }
                return Ok(category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getCategoriesByIds")]
        public async Task<IActionResult> GetCategoriesByIds([FromQuery] List<int> categoryIds)
        {
            try
            {
                if (categoryIds == null || !categoryIds.Any())
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "No category IDs provided" });
                }
                var categories = await _context.Categories
                    .Where(c => categoryIds.Contains(c.Id))
                    .ToListAsync();
                if (categories.Count == 0)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "No categories found for the provided IDs" });
                }
                return Ok(categories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getProductsByCategoryId/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategoryId(int categoryId, [FromQuery] GetItems<Product> getItems, [FromQuery] List<int>? excludeProductsIds = null, [FromQuery] bool IsAdmin = false)
        {
            // -1 is for OnOffer
            try
            {
                excludeProductsIds = excludeProductsIds ?? [];

                var query = _context.Products
                    .Where(p => (categoryId != -1 ? p.CategoryId == categoryId : p.DiscountedPrice > 0) && !excludeProductsIds.Contains(p.Id) &&
                     (!IsAdmin ? p.Quantity > 0 : true))
                    .Include(p => p.Category)
                    .Include(p => p.Manufacturer)
                    .Include(p => p.TaxRate)
                    .Include(p => p.ProductGroup)
                    .OrderBy(p => Guid.NewGuid())
                    .Take(getItems.PageSize);

                var products = await query
                    .ToListAsync();

                int allCount = await _context.Products.Where(p => p.CategoryId == categoryId)
                    .CountAsync();

                if (products.Count == 0 && (getItems.PageSize + excludeProductsIds.Count) >= allCount)
                {
                    getItems.AllItemsLoaded = true;
                }

                getItems.Items = products!;

                return Ok(getItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
