using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
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
        [HttpPost("getCategories")]
        public async Task<IActionResult> GetCategories(GetItems<Categories> getItems)
        {
            try
            {

               IQueryable<Categories> query;
                // wenn admin darf alle Produkte sehen
                if (getItems.IsAdmin)
                {
                    query = _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name_de);
                }
                // wenn eine normale Benutzer dann darf nur die Categories, die quantity haben
                else
                {
                    query = _context.Categories
                        .AsNoTracking()
                        .OrderBy(c => c.Name_de)
                        .Where(c => _context.Products.Any(p => p.CategoryId == c.Id && p.Quantity > 0));
                }

                var categories = await query
                    .ToListAsync();

                getItems.Items = categories;
                getItems.AllItemsLoaded = true;


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
        [HttpPost("getProductsByCategoryId")]
        public async Task<IActionResult> GetProductsByCategoryId([FromBody] GetItems<Product> getItems)
        {
            // -1 is for OnOffer
            try
            {

                if(getItems?.Filter == null || getItems.Filter.Type != GetItemFilterType.Category)
                    return StatusCode(500, new ValidationResult { Result = false, Message = "die filterType ist nicht category, hier muss category sein" });

                int categoryId = getItems.Filter.Id;

                // initize
                getItems.ExcludeProductsIds ??= [];
                DateTime today = DateTime.Today;
                //
                var baseQuery = _context.Products
                    .AsNoTracking()
                    .Where(p => (categoryId == -1 ?
                       (p.ProductDiscount != null &&
                        p.ProductDiscount.DiscountedPrice > 0 &&
                        today >= p.ProductDiscount.StartDate.Date &&
                        today <= p.ProductDiscount.EndDate.Date)
                        : p.CategoryId == categoryId)
                    && !getItems.ExcludeProductsIds.Contains(p.Id) &&
                     (getItems.IsAdmin || p.Quantity > 0));

                // get favoriteids um zu vergleichen
                var favoriteIds = await _context.UserFavorite
                        .Where(x => x.UserId == getItems.UserId)
                        .Select(x => x.ProductId)
                        .ToHashSetAsync();
                // get products
                bool checkIncludeAll = getItems.Includes == ProductIncludes.All;
                bool checkIncludeCategory = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.Category);
                bool checkIncludeSuppliers = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.Suppliers);
                bool checkIncludeTaxRate = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.TaxRate);
                bool checkIncludeProductGroup = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.ProductGroup);
                bool checkIncludeProductImages = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.ProductImages);
                bool checkIncludeProductDiscount = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.ProductDiscount);

                var products = await baseQuery
                    .OrderBy(p => Guid.NewGuid())
                    .Take(getItems.PageSize)
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

                        Category = checkIncludeCategory ? p.Category : null!,
                        Suppliers = checkIncludeSuppliers ? p.Suppliers : null!,
                        TaxRate = checkIncludeTaxRate ? p.TaxRate : null,
                        ProductGroup = checkIncludeProductGroup ? p.ProductGroup : null!,
                        ProductImages = checkIncludeProductImages ? p.ProductImages : null!,
                        ProductDiscount = checkIncludeProductDiscount ? p.ProductDiscount : null!,


                        IsFavorite = favoriteIds.Contains(p.Id)
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
