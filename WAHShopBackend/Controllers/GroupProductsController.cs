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
        [HttpGet("getAllGroupProducts")]
        public async Task<IActionResult> GetAllGroupProducts()
        {
            try
            {
                var groupProducts = await _context.GroupProducts
                    .OrderBy(g => g.Id)
                    .ToListAsync();
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
        [HttpGet("getProductsByGroupProductsId/{groupProductsId}")]
        public async Task<IActionResult> GetProductsByGroupProductsId(int groupProductsId, [FromQuery] GetItems<Product>? getItems = null, [FromQuery] List<int>? excludeProductsIds = null)
        {
            getItems ??= new();

            excludeProductsIds ??= [];
            if (groupProductsId <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Invalid group products ID." });
            }

            try
            {
                var products = await _context.Products
                    .Where(p => p.ProductGroupID == groupProductsId && !excludeProductsIds.Contains(p.Id) &&
                    p.Quantity > 0)
                    .Include(p => p.ProductImages)
                    .ToListAsync();
                if (products == null || products.Count == 0)
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
        [HttpPost("createGroupProduct")]
        public async Task<IActionResult> CreateGroupProduct([FromBody] GroupProducts groupProduct)
        {
            if (groupProduct == null || (string.IsNullOrWhiteSpace(groupProduct.GroupName_de) && string.IsNullOrWhiteSpace(groupProduct.GroupName_ar)))
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Gruppenproduktdaten." });
            }
            try
            {
                await _context.GroupProducts.AddAsync(groupProduct);
                await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = true, Message = $"Gruppenprodukte erfolgreich erstellt, Id:{groupProduct.Id}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPut("updateGroupProduct")]
        public async Task<IActionResult> UpdateGroupProducts([FromBody] GroupProducts groupProduct)
        {
            if (groupProduct == null || groupProduct.Id <= 0 || (string.IsNullOrWhiteSpace(groupProduct.GroupName_de) && string.IsNullOrWhiteSpace(groupProduct.GroupName_ar)))
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Gruppenproduktdaten." });
            }
            try
            {
                var existingGroupProduct = await _context.GroupProducts.FindAsync(groupProduct.Id);
                if (existingGroupProduct == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Gruppenprodukte nicht gefunden." });
                }
                _context.Entry(existingGroupProduct).CurrentValues.SetValues(groupProduct);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, Message = "Gruppenprodukte erfolgreich aktualisiert." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Gruppenprodukte-Aktualisierung fehlgeschlagen" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpDelete("deleteGroupProducts/{groupProductsId}")]
        public async Task<IActionResult> DeleteGroupProducts(int groupProductsId)
        {
            if (groupProductsId <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Gruppenprodukt-ID." });
            }
            try
            {
                var groupProduct = await _context.GroupProducts.FindAsync(groupProductsId);
                if (groupProduct == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Gruppenprodukte nicht gefunden." });
                }
                _context.GroupProducts.Remove(groupProduct);
                await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = true, Message = "Gruppenprodukte erfolgreich gelöscht." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
