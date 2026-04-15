using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
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
                if (result > 0)
                    return Ok(new ValidationResult { Result = true, Message = $"Rabattcode successfully added", NewId = newDiscountCodes.Id });
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Fehler beim Hinzufügen der Rabattcode." });

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
                return BadRequest(new ValidationResult { Result = false, Message = "Das DiscountCategory darf nicht null sein." });
            try
            {
                _context.DiscountCategory.Add(newDiscountCategory);
                int result = await _context.SaveChangesAsync();
                if(result > 0)
                    return Ok(new ValidationResult { Result = true, Message = $"Rabattkategorie  erfolgreich hinzugeüft", NewId = newDiscountCategory.Id });
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Fehler beim Hinzufügen der Rabattkategorie." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpDelete("deleteDiscountCode/{id}")]
        public async Task<IActionResult> DeleteDiscountCode(int id)
        {
            try
            {
                var discountCode = await _context.DiscountCodes.FindAsync(id);
                if (discountCode == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Discount code not found." });
                _context.DiscountCodes.Remove(discountCode);
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Discount code with ID: {id} deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpDelete("deleteDiscountCategory/{id}")]
        public async Task<IActionResult> DeleteDiscountCategory(int id)
        {
            try
            {
                var discountCategory = await _context.DiscountCategory.FindAsync(id);
                if (discountCategory == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Discount category not found." });
                _context.DiscountCategory.Remove(discountCategory);
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Discount category with ID: {id} deleted." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPut("updateDiscountCode")]
        public async Task<IActionResult> UpdateDiscountCode([FromBody] DiscountCodes updatedDiscountCode)
        {
            if (updatedDiscountCode == null || updatedDiscountCode.Id <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Invalid discount code data." });
            try
            {
                var existingDiscountCode = await _context.DiscountCodes.FindAsync(updatedDiscountCode.Id);
                if (existingDiscountCode == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Discount code not found." });
                // Update properties
                existingDiscountCode.Code = updatedDiscountCode.Code;
                existingDiscountCode.DiscountAmount = updatedDiscountCode.DiscountAmount;
                existingDiscountCode.UsageLimit = updatedDiscountCode.UsageLimit;
                existingDiscountCode.TimesUsed = updatedDiscountCode.TimesUsed;
                existingDiscountCode.IsActive = updatedDiscountCode.IsActive;
                existingDiscountCode.StartDate = updatedDiscountCode.StartDate;
                existingDiscountCode.EndDate = updatedDiscountCode.EndDate;
                existingDiscountCode.DiscountType = updatedDiscountCode.DiscountType;
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Discount code with ID: {updatedDiscountCode.Id} updated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPut("updateDiscountCategory")]
        public async Task<IActionResult> UpdateDiscountCategory([FromBody] DiscountCategory updatedDiscountCategory)
        {
            if (updatedDiscountCategory == null || updatedDiscountCategory.Id <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Invalid discount category data." });
            try
            {
                var existingDiscountCategory = await _context.DiscountCategory.FindAsync(updatedDiscountCategory.Id);
                if (existingDiscountCategory == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Discount category not found." });
                // Update properties
                existingDiscountCategory.Code = updatedDiscountCategory.Code;
                existingDiscountCategory.DiscountAmount = updatedDiscountCategory.DiscountAmount;
                existingDiscountCategory.UsageLimit = updatedDiscountCategory.UsageLimit;
                existingDiscountCategory.TimesUsed = updatedDiscountCategory.TimesUsed;
                existingDiscountCategory.IsActive = updatedDiscountCategory.IsActive;
                existingDiscountCategory.StartDate = updatedDiscountCategory.StartDate;
                existingDiscountCategory.EndDate = updatedDiscountCategory.EndDate;
                existingDiscountCategory.CategoriesId = updatedDiscountCategory.CategoriesId;
                existingDiscountCategory.DiscountType = updatedDiscountCategory.DiscountType;
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Discount category with ID: {updatedDiscountCategory.Id} updated." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("checkDiscountCode/{code}/{userId}")]
        public async Task<IActionResult> CheckDiscountCode(string code, int userId)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest(new ValidationResult { Result = false, Message = "Der Rabattcode darf nicht null oder leer sein." });
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
                    return NotFound(new ValidationResult { Result = false, Message = "Rabattcode nicht gefunden oder inaktiv." });

                var isAlreadyUsedByUser = await _context.UsedDiscountCodes
                    .AnyAsync(u => u.UserId == userId && u.DiscountCodeId == discountCode.Id);

                if (isAlreadyUsedByUser)
                {
                    return BadRequest(new ValidationResult { Result = false, Message = "Sie haben diesen Code bereits einmal verwendet"}); }

                return Ok(discountCode);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("checkDiscountCategory/{code}/{userId}")]
        public async Task<IActionResult> CheckDiscountCategory(string code, int userId)
        {
            if (string.IsNullOrEmpty(code))
                return BadRequest(new ValidationResult { Result = false, Message = "Der Rabattcode darf nicht null oder leer sein." });
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

                var isAlreadyUsedByUser = await _context.UsedDiscountCodes
                   .AnyAsync(u => u.UserId == userId && u.DiscountCodeId == discountCategory.Id);

                if (isAlreadyUsedByUser)
                {
                    return BadRequest(new ValidationResult{ Result = false, Message = "Sie haben diesen Code bereits einmal verwendet" });
                }

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
    
