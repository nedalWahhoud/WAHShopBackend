using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WAHShopBackend.Data;
using WAHShopBackend.Models;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController(MyDbContext context, IOptions<JwtSettings> jwtSettings) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        private readonly IOptions<JwtSettings> _jwtSettings = jwtSettings;
        // customers APIs
        [HttpGet("getAllCustomers")]
        public async Task<IActionResult> GetAllCustomers()
        {
            try
            {
                var customers = await _context.Customers
                    .OrderBy(c => c.DistributionLineId)
                    .ThenBy(c => c.StopNumber)
                    .Include(c => c.DistributionLine)
                    .ToListAsync();
                if (customers == null || customers.Count == 0)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Keine Kunden gefunden." });
                }
                return Ok(customers);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPost("addCustomer")]
        public async Task<IActionResult> AddCustomer([FromBody] Customers customer)
        {
            if (customer == null)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Kundendaten sind null" });
            }

            try
            {
                IActionResult actionResult = null!;
                var strategy = _context.Database.CreateExecutionStrategy();

                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    // wenn nötig bearbeiten die stopnumber
                    if(customer.shouldStopnummerShift)
                        await ShiftStopNumbersAsync(customer.DistributionLineId, customer.StopNumber);

                    //  Add customer
                    _context.Customers.Add(customer);

                    var result = await _context.SaveChangesAsync();

                    if (result <= 0)
                    {
                        actionResult = StatusCode(500, new ValidationResult
                        {
                            Result = false,
                            Message = "Die Erstellung des Kunden ist fehlgeschlagen."
                        });
                    }
                    else
                    {
                        await transaction.CommitAsync();
                        actionResult = Ok(new ValidationResult
                        {
                            Result = true,
                            Message = $"Kunde erfolgreich erstellt.",
                            NewId = customer.Id
                        });
                    }
                });

                return actionResult!;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult
                {
                    Result = false,
                    Message = ex.InnerException?.ToString() ?? ex.Message
                });
            }
        }
        [HttpPut("updateCustomer")]
        public async Task<IActionResult> UpdateCustomer([FromBody] Customers customer)
        {
            if (customer == null || customer.Id <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Kundendaten." });
            }
            try
            {
                var existingCustomer = await _context.Customers.FindAsync(customer.Id);
                if (existingCustomer == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Kunde nicht gefunden." });
                }

                _context.Entry(existingCustomer).CurrentValues.SetValues(customer);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, Message = "Kunde erfolgreich aktualisiert." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Die Aktualisierung des Kunden ist fehlgeschlagen." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        private async Task ShiftStopNumbersAsync(int distributionLineId,int newStopNumber,int? oldStopNumber = null)
        {
            // Neuer Erstellungsstatus (oldStopNumber == null)
            if (oldStopNumber == null)
            {
                var customersToShift = await _context.Customers
                    .Where(c =>
                        c.DistributionLineId == distributionLineId &&
                        c.StopNumber >= newStopNumber)
                    .OrderByDescending(c => c.StopNumber)
                    .ToListAsync();

                foreach (var c in customersToShift)
                    c.StopNumber += 1;
            }
            else
            {
                // wenn update, in zukünft konfigrieren

                // Wenn sich die Zahl nicht ändert, tun wir nichts.
                if (oldStopNumber == newStopNumber)
                    return;
            }
        }
        [HttpDelete("deleteCustomer/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Kunden Id." });
            }
            try
            {
                var existingCustomer = await _context.Customers.FindAsync(id);
                if (existingCustomer == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Kunde nicht gefunden." });
                }
                _context.Customers.Remove(existingCustomer);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, Message = "Kunde erfolgreich gelöscht." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Die Löschung des Kunden ist fehlgeschlagen." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getCustomerById/{id}")]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            if (id <= 0)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Kunden Id." });
            }
            try
            {
                var customer = await _context.Customers
                    .Include(c => c.DistributionLine)
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (customer == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Kunde nicht gefunden." });
                }
                return Ok(customer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        // login
        [HttpPost("customerLogin")]
        public async Task<IActionResult> Login(CustomerLoginDto dto)
        {
            try
            {
                var customer = await _context.Customers
                               .FirstOrDefaultAsync(c =>
                               (c.PhoneNumber == dto.PhoneNumber || c.Id == dto.Id) 
                               && c.PIN == dto.PIN);

                if (customer == null)
                    return Unauthorized(new ValidationResult { Result = false, Message = "Ungültige Telefonnummer oder PIN" });

                return Ok(new ValidationResult { Result = true, Message = GetCustomerToken(customer.Id) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        private string GetCustomerToken(int customerId)
        {
            var claims = new[]
            {
               new Claim(ClaimTypes.NameIdentifier, customerId.ToString()),
               new Claim("role", "Customer"),
             };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Value.Key!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Value.Issuer,
                audience: _jwtSettings.Value.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
