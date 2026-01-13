using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DistributionLinesController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        // distribution lines APIs
        [HttpGet("getAllDistributionLines")]
        public async Task<IActionResult> GetAllDistributionLines()
        {
            try
            {
                var distributionLines = await _context.DistributionLines
                    .ToListAsync();
                if (distributionLines == null || distributionLines.Count == 0)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Keine Verteilungslinien gefunden." });
                }

                return Ok(distributionLines);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPost("addDistributionLine")]
        public async Task<IActionResult> AddDistributionLines([FromBody] DistributionLines distributionLines)
        {
            if (distributionLines == null)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "DistributionLines data ist null" });
            }
            try
            {
                _context.DistributionLines.Add(distributionLines);
                var result = await _context.SaveChangesAsync();
                if (result <= 0)
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Die Erstellung der Verteilungslinien ist fehlgeschlagen." });

                return Ok(new ValidationResult { Result = true, Message = $"Verteilungslinien erfolgreich erstellt, Id:{distributionLines.Id}." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPut("updateDistributionLine")]
        public async Task<IActionResult> UpdateDistributionLines([FromBody] DistributionLines distributionLines)
        {
            if (distributionLines == null || distributionLines.Id <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige DistributionLines-Daten." });
            }
            try
            {
                var existingDistributionLines = await _context.DistributionLines.FindAsync(distributionLines.Id);
                if (existingDistributionLines == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "DistributionLines nicht gefunden." });
                }
                _context.Entry(existingDistributionLines).CurrentValues.SetValues(distributionLines);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, Message = "DistributionLines erfolgreich aktualisiert." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Die Aktualisierung der Verteilungslinien ist fehlgeschlagen." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpDelete("deleteDistributionLine/{id}")]
        public async Task<IActionResult> DeleteDistributionLines(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige DistributionLines Id." });
            }
            try
            {
                var existingDistributionLines = await _context.DistributionLines.FindAsync(id);
                if (existingDistributionLines == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "DistributionLines nicht gefunden." });
                }
                _context.DistributionLines.Remove(existingDistributionLines);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, Message = "DistributionLines erfolgreich gelöscht." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Die Löschung der Verteilungslinien ist fehlgeschlagen." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
