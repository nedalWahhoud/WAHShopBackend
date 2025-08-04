using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;

        [HttpGet("getBankTransferDetails")]
        public async Task<IActionResult> GetBankTransferDetails()
        {
            try
            {
                var bankTransferDetails = await _context.BankTransferDetails.ToListAsync();
                if (bankTransferDetails == null || bankTransferDetails.Count == 0)
                {
                    return NotFound(new ValidationResult() { Result = false, Message = "Bank transfer details not found." });
                }
                return Ok(bankTransferDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"Beim get der BankTransferDetails ist ein Fehler aufgetreten: {ex.Message}" });
            }
        }
    }
}
