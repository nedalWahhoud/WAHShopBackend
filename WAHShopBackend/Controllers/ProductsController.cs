using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
using WAHShopBackend.ImagesF;
using WAHShopBackend.ProductP;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(MyDbContext context,ProductService productService, ProductImagesService productImagesService) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        private readonly ProductImagesService _productImagesService = productImagesService;
        private readonly ProductService _productService = productService;

        [HttpPost("addProduct")]
        public async Task<IActionResult> AddProduct([FromBody] Product newProduct)
        {
            if (newProduct == null)
                return BadRequest(new ValidationResult { Result = false, Message = "Product cannot be null." });

            try
            {
                string Message = string.Empty;
                bool isValid = _productService.IsValidProduct(newProduct!, out Message);
                if (!isValid)
                    return BadRequest(new ValidationResult { Result = false, Message = Message });

                _context.Products.Add(newProduct);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    if (newProduct.ProductImages != null && newProduct.ProductImages.Count > 0)
                    {
                        var resultImage = _productImagesService.AddImage(newProduct);
                        if (resultImage.Result == true)
                        {
                            var result1 = await _context.SaveChangesAsync();
                            if (result1 == 0)
                            {
                                await DeleteProduct(newProduct.Id);
                                _productImagesService.DeleteImage(newProduct.Id, true);
                                return StatusCode(500, new ValidationResult { Result = false, Message = "Das Produkt wurde hinzugefügt (aber wieder gelöscht), das Bild konnte nicht in der Datenbank gespeichert werden." });
                            }
                            return Ok(new ValidationResult { Result = true, Message = $"Id:{newProduct.Id}" });
                        }
                        else
                        {
                            await DeleteProduct(newProduct.Id);
                            return StatusCode(500, new ValidationResult { Result = false, Message = "Das Produkt wurde hinzugefügt (aber wieder gelöscht), das Bild konnte nicht gespeichert werden: " + resultImage.Message });
                        }
                    }
                    else
                    {
                        await DeleteProduct(newProduct.Id);
                        return StatusCode(500,new ValidationResult { Result = false, Message = $"ProductImages array ist null" });
                    }
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Das Produkt konnte nicht hinzugefügt werden." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        
        [HttpPost("getProductByIds")]
        public async Task<IActionResult> GetProductByIds([FromBody] List<int> ids)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
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
        [HttpGet("getProductById/{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Manufacturer)
                    .Include(p => p.TaxRate)
                    .Include(p => p.ProductGroup)
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (product != null)
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
                    .Include(p => p.ProductImages)
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
                var existingProduct = await _context.Products
                    .Include(p => p.ProductImages)
                    .FirstOrDefaultAsync(p => p.Id == editProduct.Id);
                if (existingProduct != null)
                {
                    if (editProduct.ProductImages != null)
                    {
                        var resultImage = _productImagesService.EditImage(existingProduct.ProductImages,editProduct.ProductImages);
                        if (resultImage.Result == false)
                        {
                            return StatusCode(500, new ValidationResult { Result = false, Message = "Bildaktualisierung fehlgeschlagen: " + resultImage.Message });
                        }
                    }

                    //
                    _context.Entry(existingProduct).CurrentValues.SetValues(editProduct);
                    _context.Entry(existingProduct).State = EntityState.Modified;
                    int result = await _context.SaveChangesAsync();
                    if (result > 0)
                    {
                        return Ok(new ValidationResult { Result = true, Message = "Produkt erfolgreich aktualisiert" });
                    }
                    else
                    {
                        return StatusCode(500, new ValidationResult { Result = false, Message = "Produktaktualisierung fehlgeschlagen" });
                    }
                }
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Product nicht gefunden" });

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
                    if(result > 0)
                    {
                        _productImagesService.DeleteImage(id, true);

                        return Ok(new ValidationResult { Result = result > 0, Message = "Product deleted successfully" });
                    }
                    else
                    {
                        return StatusCode(500, new ValidationResult { Result = false, Message = "Failed to delete product" });
                    }

                }
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Product not found" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
