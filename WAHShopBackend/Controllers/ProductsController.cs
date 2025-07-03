using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
using WAHShopBackend.ProductP;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(MyDbContext context) : ControllerBase
    {
        [HttpPost("addProduct")]
        public async Task<IActionResult> AddProduct([FromBody] Product newProduct)
        {
            if (newProduct == null)
                return BadRequest(new ValidationResult { Result = false, Message = "Product cannot be null." });

            try
            {
                string Message = string.Empty;
                bool isValid = ProductService.IsValidProduct(newProduct!, out Message);
                if (!isValid)
                    return BadRequest(new ValidationResult { Result = false, Message = Message });

                _context.Products.Add(newProduct);
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Id:{newProduct.Id}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        private readonly MyDbContext _context = context;
        [HttpPost("getProductByIds")]
        public async Task<IActionResult> GetProductByIds([FromBody] List<int> ids)
        {
            try
            {
                var product = await _context.Products
                    .Include(p=>p.ProductGroup)
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync();
                if (product != null && product.Count > 0)
                    return Ok(product);
                else
                    return BadRequest(new ValidationResult { Result = false, Message = "Product not found." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getProducts")]
        public async Task<IActionResult> GetProducts([FromQuery] GetItems<Product> _getItems)
        {
            GetItems<Product> getItems = new();

            if (_getItems.AllItemsLoaded) return Ok(getItems = new GetItems<Product>
            {
                Items = [],
                AllItemsLoaded = true
            });

            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Manufacturer)
                    .Include(p => p.TaxRate)
                    .Include(p => p.ProductGroup)
                    .OrderByDescending(p => p.Id)
                    .Skip(_getItems.CurrentPage * _getItems.PageSize)
                    .Take(_getItems.PageSize)
                    .ToListAsync();

                if (products.Count == 0)
                {
                    getItems.AllItemsLoaded = true;
                }

                _getItems.CurrentPage++;
                getItems.Items = products;

                getItems.CurrentPage = _getItems.CurrentPage;

                return Ok(getItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getCategories")]
        public async Task<ActionResult<GetItems<Categories>>> GetCategories()
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
                return StatusCode(500, new { error = "Internal server error" });
            }
        }
        [HttpGet("getManufacturers")]
        public async Task<ActionResult<GetItems<Manufacturers>>> GetManufacturers()
        {
            try
            {
                var manufacturers = await _context.Manufacturers.ToListAsync();

                var getItems = new GetItems<Manufacturers>
                {
                    Items = manufacturers,
                    AllItemsLoaded = true
                };

                return Ok(getItems);
            }
            catch
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error" });
            }
        }
        [HttpGet("getTaxRates")]
        public async Task<IActionResult> GetTaxRates()
        {
            try
            {
                var taxRates = await _context.TaxRates.ToListAsync();

                var getItems = new GetItems<TaxRate>
                {
                    Items = taxRates,
                    AllItemsLoaded = true
                };

                return Ok(getItems);
            }
            catch
            {
                return StatusCode(500, new ValidationResult { Result = false , Message = "Internal server error" });
            }
        }
        [HttpPost("updateProduct")]
        public async Task<IActionResult> UpdateProduct([FromBody] Product editProduct)
        {
            try
            {
                var existingProduct = await _context.Products.FindAsync(editProduct.Id);
                if (existingProduct != null)
                {
                    _context.Entry(existingProduct).CurrentValues.SetValues(editProduct);
                    int result = await _context.SaveChangesAsync();
                    return Ok(new ValidationResult { Result = result > 0, Message = "Product updated successfully" });
                }
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Product not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpDelete("deleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var existingProduct = await _context.Products.FindAsync(id);
                if (existingProduct != null)
                {
                    _context.Products.Remove(existingProduct);
                    int result = await _context.SaveChangesAsync();
                    return Ok(new ValidationResult { Result = result > 0, Message = "Product deleted successfully" });
                }
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Product not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
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
                    .Include (p => p.Category)
                    .Include(p => p.TaxRate)
                    .Include(p=> p.ProductGroup)
                    .Where(p => p.Quantity > 0 &&
                                !excludeIds!.Contains(p.Id) &&
                                (p.Name_de!.ToLower().Contains(query.ToLower()) ||
                                p.Description_de!.ToLower().Contains(query.ToLower()) ||
                                p.Name_ar!.ToLower().Contains(query.ToLower()) ||
                                p.Description_ar!.ToLower().Contains(query.ToLower()) ||
                                p.Category != null && p.Category.Name_de!.ToLower().Contains(query.ToLower()) ||
                                p.Category != null && p.Category.Name_ar!.ToLower().Contains(query.ToLower())))
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
