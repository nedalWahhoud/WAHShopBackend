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
        [HttpPost("createCategory")]
        public async Task<IActionResult> CreateCategory([FromBody] Categories category)
        {
            if (category == null) 
                return BadRequest(new ValidationResult { Result = false, Message = "Category data is null" });

            try
            {
                _context.Categories.Add(category);
                int result = await _context.SaveChangesAsync();
                if(result > 0)
                    return Ok(new ValidationResult { Result = true, NewId = category.Id });
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Kategorie konnte nicht erstellt werden" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPut("updateCategory")]
        public async Task<IActionResult> UpdateCategory([FromBody] Categories category)
        {
            if (category == null || category.Id <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Categorydaten." });
            }
            try
            {
                var existingCategory = await _context.Categories.FindAsync(category.Id);
                if (existingCategory == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Category nicht gefunden." });
                }

                _context.Entry(existingCategory).CurrentValues.SetValues(category);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, Message = "Category erfolgreich aktualisiert." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Category-Aktualisierung fehlgeschlagen" });
                }
            }
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new ValidationResult { Result = false, Message = "Der Lieferant wurde von einem anderen Prozess aktualisiert. Bitte laden Sie die Daten erneut und versuchen Sie es erneut." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpDelete("deleteCategory/{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Category nicht gefunden." });
                }
                _context.Categories.Remove(category);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, Message = "Category erfolgreich gelöscht." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Category-Löschung fehlgeschlagen" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getCategories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .OrderBy(c => c.Name_de)
                    .ToListAsync();

                var getItems = new GetItems<Categories>
                {
                    Items = categories,
                    AllItemsLoaded = true
                };

                return Ok(getItems);
            }
            catch(Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
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
                if (categoryIds == null || categoryIds.Count == 0)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "No category IDs provided" });
                }
                var categories = await _context.Categories
                    .Where(c => categoryIds.Contains(c.Id))
                    .OrderBy(c => c.Name_de)
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
        [HttpPost("getProductsByCategoryId/{categoryId}")]
        public async Task<IActionResult> GetProductsByCategoryId([FromRoute] int categoryId, [FromBody] GetItems<Product> getItems)
        {
            // -1 is for OnOffer
            try
            {
                getItems.ExcludeProductsIds ??= [];

                var baseQuery = _context.Products
                    .AsNoTracking()
                    .Where(p => (categoryId == -1 ?
                       (p.ProductDiscount != null &&
                        p.ProductDiscount.DiscountedPrice > 0 &&
                        DateTime.Today >= p.ProductDiscount.StartDate.Date &&
                        DateTime.Today <= p.ProductDiscount.EndDate.Date)
                        : p.CategoryId == categoryId)
                    && !getItems.ExcludeProductsIds.Contains(p.Id) &&
                     (getItems.IsAdmin || p.Quantity > 0));

                // random
                var availableIds = await baseQuery.Select(p => p.Id).ToListAsync();
                var random = new Random();
                var randomIds = availableIds
                    .OrderBy(id => random.Next())
                    .Take(getItems.PageSize)
                    .ToList();
                // get products
                bool includeAll = getItems.Includes == ProductIncludes.All;
                var products = await baseQuery
                    .Where(p => randomIds.Contains(p.Id))
                    .Select(p => new Product
                    {
                        Id = p.Id,
                        Name_de = p.Name_de,
                        Description_de = p.Description_de,
                        CategoryId = p.CategoryId,
                        Barcode = p.Barcode,
                        Quantity = p.Quantity,
                        PurchasePrice = p.PurchasePrice,
                        SalePrice = p.SalePrice,
                        MinimumStock = p.MinimumStock,
                        EXPDate = p.EXPDate,
                        UserId = p.UserId,
                        Name_ar = p.Name_ar,
                        Description_ar = p.Description_ar,
                        TaxRateId = p.TaxRateId,
                        ProductGroupID = p.ProductGroupID,
                        IsShippable = p.IsShippable,

                        Category = getItems.Includes.HasFlag(ProductIncludes.Category) || includeAll ? p.Category : null!,
                        Suppliers = getItems.Includes.HasFlag(ProductIncludes.Suppliers) || includeAll ? p.Suppliers : null!,
                        TaxRate = getItems.Includes.HasFlag(ProductIncludes.TaxRate) || includeAll ? p.TaxRate : null,
                        ProductGroup = getItems.Includes.HasFlag(ProductIncludes.ProductGroup) || includeAll ? p.ProductGroup : null!,
                        ProductImages = getItems.Includes.HasFlag(ProductIncludes.ProductImages) || includeAll ? p.ProductImages : null!,
                        ProductDiscount = getItems.Includes.HasFlag(ProductIncludes.ProductDiscount) || includeAll ? p.ProductDiscount : null!,


                        IsFavorite = (getItems.UserId > 0 ? _context.UserFavorite.Any(f => f.ProductId == p.Id && f.UserId == getItems.UserId) : false)
                    })
                    .ToListAsync();

                int allCount = await baseQuery.CountAsync();

                if (products.Count == 0 && (getItems.PageSize + getItems.ExcludeProductsIds.Count) >= allCount)
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
