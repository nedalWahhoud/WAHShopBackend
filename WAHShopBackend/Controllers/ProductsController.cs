using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
using WAHShopBackend.ImagesF;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(MyDbContext context,ProductImagesService productImagesService) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        private readonly ProductImagesService _productImagesService = productImagesService;

        [HttpPost("addProduct")]
        public async Task<IActionResult> AddProduct([FromBody] Product newProduct)
        {
            if (newProduct == null)
                return BadRequest(new ValidationResult { Result = false, Message = "Product cannot be null." });

            try
            {

                if (newProduct.SelectedSupplierIds != null && newProduct.SelectedSupplierIds.Count != 0)
                {
                    
                    newProduct.Suppliers = await _context.Suppliers
                        .Where(s => newProduct.SelectedSupplierIds.Contains(s.Id))
                        .ToListAsync();
                }

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
                            return Ok(new ValidationResult { Result = true, Message = "Erfolgreich erstellt", NewId = newProduct.Id });
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
                        return StatusCode(500, new ValidationResult { Result = false, Message = $"ProductImages array ist null" });
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
                    .AsNoTracking()
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductGroup)
                    .Include(p => p.ProductDiscount)
                    .Where(p => ids.Contains(p.Id))
                    .OrderBy(c => c.Name_de)
                    .ToListAsync();
                if (product != null && product.Count > 0)
                    return Ok(product);
                else
                    return BadRequest(new ValidationResult { Result = false, Message = "Keine Produkte gefunden." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getProductById/{id}")]
        public async Task<IActionResult> GetProductById(int id, bool onlyInStock = false,int userId = 0)
        {
            try
            {
                var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Suppliers)
            .Include(p => p.TaxRate)
            .Include(p => p.ProductGroup)
            .Include(p => p.ProductImages)
            .Include(p => p.ProductDiscount)
            .Select(p => new Product
            {
                Id = p.Id,
                Name_de = p.Name_de,
                Description_de = p.Description_de,
                CategoryId = p.CategoryId,
                Category = p.Category,
                Barcode = p.Barcode,
                Quantity = p.Quantity,
                PurchasePrice = p.PurchasePrice,
                SalePrice = p.SalePrice,
                MinimumStock = p.MinimumStock,
                EXPDate = p.EXPDate,
                Suppliers = p.Suppliers,
                UserId = p.UserId,
                ProductImages = p.ProductImages,
                Name_ar = p.Name_ar,
                Description_ar = p.Description_ar,
                TaxRateId = p.TaxRateId,
                TaxRate = p.TaxRate,
                ProductGroupID = p.ProductGroupID,
                ProductGroup = p.ProductGroup,
                IsShippable = p.IsShippable,
                ProductDiscount = p.ProductDiscount,
                PackagingUnit = p.PackagingUnit,
                ItemsPerPackage = p.ItemsPerPackage,
                IsFavorite = (userId > 0 ? _context.UserFavorite.Any(f => f.ProductId == p.Id && f.UserId == userId) : false)
            })
            .Where(p => p.Id == id);


                if (onlyInStock)
                {
                    query = query.Where(p => p.Quantity > 0);
                }

                var product = await query.FirstOrDefaultAsync();

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
        [HttpPost("getProducts")]
        public async Task<IActionResult> GetProducts([FromBody] GetItems<Product> getItems)
        {

            if (getItems.AllItemsLoaded)
                return Ok(getItems = new GetItems<Product> { Items = [], AllItemsLoaded = true });

            try
            {
                var query = _context.Products
                    .AsNoTracking()
                    .AsSplitQuery();

                int filterId = getItems.Filter?.Id ?? 0;
                if (getItems.Filter != null && getItems.Filter.Type != GetItemFilterType.None)
                {
                    query = getItems.Filter.Type switch
                    {
                        GetItemFilterType.Category => query.Where(p => p.CategoryId == filterId),
                        GetItemFilterType.Supplier => query.Where(p => p.Suppliers.Any(s => s.Id == filterId)),
                        GetItemFilterType.LowStock => query.Where(p => p.Quantity <= p.MinimumStock),
                        GetItemFilterType.OnOffer => query.Where(p => p.ProductDiscount != null &&
                                       p.ProductDiscount.DiscountedPrice > 0 &&
                                       DateTime.Today >= p.ProductDiscount.StartDate &&
                                       DateTime.Today <= p.ProductDiscount.EndDate),
                        _ => query
                    };
                }

                //
                bool includeAll = getItems.Includes == ProductIncludes.All;
                bool checkIncludeAll = getItems.Includes == ProductIncludes.All;
                bool checkIncludeCategory = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.Category);
                bool checkIncludeSuppliers = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.Suppliers);
                bool checkIncludeTaxRate = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.TaxRate);
                bool checkIncludeProductGroup = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.ProductGroup);
                bool checkIncludeProductImages = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.ProductImages);
                bool checkIncludeProductDiscount = checkIncludeAll || getItems.Includes.HasFlag(ProductIncludes.ProductDiscount);

                var products = await query
                    .OrderBy(p => p.Name_de)
                    .Skip(getItems.CurrentPage * getItems.PageSize)
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
                        PackagingUnit = p.PackagingUnit,
                        ItemsPerPackage = p.ItemsPerPackage,

                        Category = checkIncludeCategory ? p.Category : null!,
                        Suppliers = checkIncludeSuppliers ? p.Suppliers : null!,
                        TaxRate = checkIncludeTaxRate ? p.TaxRate : null,
                        ProductGroup = checkIncludeProductGroup ? p.ProductGroup : null!,
                        ProductImages = checkIncludeProductImages ? p.ProductImages : null!,
                        ProductDiscount = checkIncludeProductDiscount ? p.ProductDiscount : null!,
                    })
                    .ToListAsync();

                var totalCount = await query.CountAsync();
                var loadedCount = (getItems.CurrentPage * getItems.PageSize) + products.Count;
                if (loadedCount >= totalCount)
                {
                    getItems.AllItemsLoaded = true;
                }

                getItems.Items = products;
                getItems.PageSize = getItems.PageSize;
                getItems.CurrentPage = getItems.CurrentPage;

                return Ok(getItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        
        [HttpPost("updateProduct")]
        public async Task<IActionResult> UpdateProduct([FromBody] Product editProduct)
        {
            try
            {
                var existingProduct = await _context.Products
                    .Include(p => p.Suppliers)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductDiscount)
                    .FirstOrDefaultAsync(p => p.Id == editProduct.Id);

                if (existingProduct == null)
                    return StatusCode(404, new ValidationResult { Result = false, Message = "Product nicht gefunden" });


                // Aktualisierung der Lieferanten
                if (editProduct.SelectedSupplierIds != null)
                {
                    var newSuppliers = await _context.Suppliers
                        .Where(s => editProduct.SelectedSupplierIds.Contains(s.Id))
                        .ToListAsync();
                    
                    existingProduct.Suppliers = newSuppliers;
                }

                // productimage
                if (editProduct.ProductImages != null)
                {
                    var resultImage = _productImagesService.EditImage(existingProduct.ProductImages, editProduct.ProductImages);
                    if (resultImage.Result == false)
                    {
                        return StatusCode(500, new ValidationResult { Result = false, Message = "Bildaktualisierung fehlgeschlagen: " + resultImage.Message });
                    }
                }
                // Product Discount 
                if (editProduct.ProductDiscount != null)
                {
                    if (existingProduct.ProductDiscount == null)
                    {
                        editProduct.ProductDiscount.ProductsId = existingProduct.Id;
                        _context.ProductDiscounts.Add(editProduct.ProductDiscount);
                    }
                    else
                    {
                        _context.Entry(existingProduct.ProductDiscount).CurrentValues.SetValues(editProduct.ProductDiscount);
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
            catch (DbUpdateConcurrencyException)
            {
                return StatusCode(409, new ValidationResult { Result = false, Message = "Der Lieferant wurde von einem anderen Prozess aktualisiert. Bitte laden Sie die Daten erneut und versuchen Sie es erneut." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
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
        
        [HttpDelete("deleteProduct/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var existingProduct = await _context.Products
                    .Include(p => p.Suppliers)
                    .FirstOrDefaultAsync(p => p.Id == id);
                if (existingProduct != null)
                {
                    _context.Products.Remove(existingProduct);
                    int result = await _context.SaveChangesAsync();
                    if (result > 0)
                    {
                        _productImagesService.DeleteImage(id, true);

                        return Ok(new ValidationResult { Result = result > 0, Message = "Produkt erfolgreich gelöscht" });
                    }
                    else
                        return StatusCode(500, new ValidationResult { Result = false, Message = "Produkt konnte nicht gelöscht werden" });
                }
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Produkt nicht gefunden" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        /* Barcode */
        [HttpPost("updateBarcode/{id}")]
        public async Task<IActionResult> UpdateBarcode(int id, [FromBody] string newBarCode)
        {
            var existingProduct = await _context.Products.FindAsync(id);
            if (existingProduct == null)
                return NotFound(new ValidationResult { Result = false, Message = $"ProductId {id} nicht gefunden" });

            if (existingProduct.Barcode == newBarCode)
                return Ok(new ValidationResult { Result = false, Message = "Der Barcode ist bereits identisch, keine Aktualisierung erforderlich." });

            try
            {
                existingProduct.Barcode = newBarCode;
                var result = await _context.SaveChangesAsync();
                if (result > 0)
                    return Ok(new ValidationResult { Result = true, Message = "Erfolg" });
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Barcode könnte nicht geupdatet werden" });
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
        [HttpGet("getByBarcode/{barcode}")]
        public async Task<IActionResult> GetProductByBarcode(string barcode)
        {
            try
            {
                var product = await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Barcode == barcode);

                if (product == null)
                    return NotFound(new ValidationResult { Result = false, Message = $"Kein Produkt mit diesem {barcode} gefunden." });

                return Ok(product);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}
