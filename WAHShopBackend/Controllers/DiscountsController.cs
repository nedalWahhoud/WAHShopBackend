using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiscountsController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        [HttpPost("addDiscountCode")]
        public async Task<IActionResult> AddDiscountCode([FromBody] DiscountCodes newDiscountCodes)
        {
            if (newDiscountCodes == null)
                return BadRequest(new ValidationResult { Result = false, Message = "Product cannot be null." });

            try
            {
                _context.DiscountCodes.Add(newDiscountCodes);
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Discount code added with ID: {newDiscountCodes.Id}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPost("addDiscountCategory")]
        public async Task<IActionResult> AddDiscountCategory([FromBody] DiscountCategory newDiscountCategory)
        {
            if (newDiscountCategory == null)
                return BadRequest(new ValidationResult { Result = false, Message = "Product cannot be null." });
            try
            {
                _context.DiscountCategory.Add(newDiscountCategory);
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Discount category added with ID: {newDiscountCategory.Id}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("checkDiscountCode/{code}")]
        public async Task<IActionResult> CheckDiscountCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest(new ValidationResult { Result = false, Message = "Discount code cannot be null or empty." });
            try
            {
                code = code.Trim();

                var discountCode = await _context.DiscountCodes
                .FirstOrDefaultAsync(dc =>
                dc.Code == code &&
                dc.UsageLimit > dc.TimesUsed &&
                dc.IsActive &&
                dc.StartDate <= DateTime.Now &&
                dc.EndDate >= DateTime.Now);

                if (discountCode == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Discount code not found or inactive." });

                return Ok(discountCode);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("checkDiscountCategory/{code}")]
        public async Task<IActionResult> CheckDiscountCategory(string code)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest(new ValidationResult { Result = false, Message = "Discount code cannot be null or empty." });
            try
            {
                code = code.Trim();
                var discountCategory = await _context.DiscountCategory
                .FirstOrDefaultAsync(dc =>
                dc.Code == code &&
                dc.UsageLimit > dc.TimesUsed &&
                dc.IsActive &&
                dc.StartDate <= DateTime.Now &&
                dc.EndDate >= DateTime.Now);
                if (discountCategory == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Rabattkategorie nicht gefunden oder inaktiv." });
                return Ok(discountCategory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }

        }
        [HttpGet("getAllDiscountCodes")]
        public async Task<IActionResult> GetAllDiscountCodes()
        {
            try
            {
                var discountCodes = await _context.DiscountCodes.ToListAsync();

                if (discountCodes == null || discountCodes.Count == 0)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Keine Discount-Codes gefunden." });
                }

                return Ok(discountCodes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getAllDiscountCategories")]
        public async Task<IActionResult> GetAllDiscountCategories()
        {
            try
            {
                var discountCategories = await _context.DiscountCategory.Include(dc => dc.Category).ToListAsync();
                if (discountCategories == null || discountCategories.Count == 0)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Keine Discount-Kategorie gefunden." });
                }
                return Ok(discountCategories);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
    
