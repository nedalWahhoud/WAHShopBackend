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
        [HttpGet("getAllCustomersByLineId/{id}")]
        public async Task<IActionResult> GetAllCustomersByLineId(int id)
        {
            try
            {
                var today = DateTime.Today;

                var customers = await _context.Customers
                    .Where(c => id == 0 || c.DistributionLineId == id)
                    .Select(c => new Customers
                    {
                        Id = c.Id,
                        Name_de = c.Name_de,
                        Name_ar = c.Name_ar,
                        Street = c.Street,
                        BuildingNumber = c.BuildingNumber,
                        PostalCode = c.PostalCode,
                        City = c.City,
                        Latitude = c.Latitude,
                        Longitude = c.Longitude,
                        PhoneNumber = c.PhoneNumber,
                        Email = c.Email,
                        Notes_de = c.Notes_de,
                        Notes_ar = c.Notes_ar,
                        CreatedAt = c.CreatedAt,
                        StopNumber = c.StopNumber,
                        DistributionLineId = c.DistributionLineId,
                        DistributionLine = c.DistributionLine,
                        PIN = c.PIN,
                        // prüfen der Kunde ob heute eine Einamlzahlung hat
                        HasOneTimePaymentToday = c.OneTimePayments.Any(p => p.PickupDate.Date == today),
                        // prüfen ob der Kunde Schulden hat
                        HasDebt = _context.DebtCustomers.Any(d => d.CustomerId == c.Id && d.Balance > 0)
                    })
                    .OrderBy(c => c.DistributionLineId)
                    .ThenBy(c => c.StopNumber)
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
                        await ShiftStopNumbersAsync(customer.DistributionLineId, customer.StopNumber,-1);

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

                IActionResult actionResult = null!;
                var strategy = _context.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();
                    // wenn nötig bearbeiten die stopnumber
                    int newStopNumber = await ShiftStopNumbersAsync(customer.DistributionLineId, customer.StopNumber, existingCustomer.StopNumber, customer.Id,false, customer.shouldStopnummerShift);

                  

                    _context.Entry(existingCustomer).CurrentValues.SetValues(customer);


                    // wenn die newStopNumber nicht -1 ist, bedeutet dass die newStopnumber von dem Kunden in FUnktion ShiftStopNumbersAsync geändert wurde, und muss die Änderung umgesetzt werden.
                    if (newStopNumber != -1)
                        existingCustomer.StopNumber = newStopNumber;

                    int result = await _context.SaveChangesAsync();
                    if (result > 0)
                    {
                        await transaction.CommitAsync();
                        actionResult = Ok(new ValidationResult { Result = true, Message = "Kunde erfolgreich aktualisiert." });
                    }
                    else
                    {
                        actionResult = StatusCode(500, new ValidationResult { Result = false, Message = "Die Aktualisierung des Kunden ist fehlgeschlagen." });
                    }
                });

                return actionResult;
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
        private async Task<int> ShiftStopNumbersAsync(int distributionLineId, int newStopNumber, int? oldStopNumber, int currentCustomerId = 0, bool isDelete = false, bool shouldStopnummerShift = true)
        {

            /* -1 bdeuetet dass die newStopNumber nix geändert und muss nixht angefasst werden */

            // prüfen ob die Stelle noch belegt ist 
            bool isStopStillOccupied = await _context.Customers
                   .AnyAsync(c => c.DistributionLineId == distributionLineId && c.StopNumber == oldStopNumber && c.Id != currentCustomerId);

            if (shouldStopnummerShift)
            {
                // löschen
                if (isDelete)
                {
                    // wenn  die Haltestelle noch belegt ist, tun wir nichts, da muss keine Verschiebung stattfinden, weil die Lücke nicht geschlossen werden muss.
                    if (isStopStillOccupied)
                        return -1;

                    var customersToShift = await _context.Customers
                    .Where(c =>
                        c.DistributionLineId == distributionLineId &&
                        c.StopNumber > oldStopNumber)
                    .OrderBy(c => c.StopNumber)
                    .ToListAsync();
                    foreach (var c in customersToShift)
                        c.StopNumber -= 1;
                    return -1;
                }

                // update
                // Neuer Erstellungsstatus (oldStopNumber == -1)
                if (oldStopNumber == -1)
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

                    // Wenn sich die Zahl nicht ändert, tun wir nichts.
                    if (oldStopNumber == newStopNumber)
                        return -1;

                    // Wenn die neue Stopnummer bereits von einem anderen Kunden belegt ist, müssen wir die Kunden ab der neuen Stopnummer um 1 nach oben verschieben, um Platz für den aktualisierten Kunden zu schaffen.
                    if (isStopStillOccupied)
                    {
                        var customersToShiftUp = await _context.Customers
                            .Where(c => c.DistributionLineId == distributionLineId &&
                                        c.StopNumber >= newStopNumber)
                            .OrderByDescending(c => c.StopNumber)
                            .ToListAsync();

                        foreach (var c in customersToShiftUp)
                        {
                            c.StopNumber += 1;
                        }
                        return -1;
                    }

                    // Erster Fall: Wechsel zu einem kleineren Punkt (z. B. von 5 auf 2)
                    if (newStopNumber < oldStopNumber)
                    {

                        // Wir möchten zwischen dem neuen Punkt (2) und dem vorherigen Punkt (4) Platz schaffen, sodass sie zu (3 bis 5) werden.
                        var customersToShift = await _context.Customers
                            .Where(c => c.DistributionLineId == distributionLineId &&
                                        c.StopNumber >= newStopNumber &&
                                        c.StopNumber < oldStopNumber)
                            .OrderByDescending(c => c.StopNumber)// Die absteigende Reihenfolge ist beim Erhöhen (+1) zwingend erforderlich, um Änderungeninterferenzen zu vermeiden.
                            .ToListAsync();

                        foreach (var c in customersToShift)
                        {
                            c.StopNumber += 1;
                        }
                    }
                    // Zweiter Fall: Wechsel zu einem größeren Punkt (z. B. von 2 auf 5)
                    else if (newStopNumber > oldStopNumber)
                    {
                        var customersToShift = await _context.Customers
                            .Where(c => c.DistributionLineId == distributionLineId &&
                                        c.StopNumber > oldStopNumber &&
                                        c.StopNumber <= newStopNumber)
                            .OrderBy(c => c.StopNumber) // Die aufsteigende Reihenfolge ist beim Absteigen wichtig (-1)
                            .ToListAsync();

                        foreach (var c in customersToShift)
                        {
                            c.StopNumber -= 1;
                        }
                    }
                }
            }
            else
            {
                if (!isStopStillOccupied)
                {
                    var customersToShift = await _context.Customers
                                       .Where(c =>
                                              c.DistributionLineId == distributionLineId &&
                                              c.StopNumber > oldStopNumber &&
                                              c.Id != currentCustomerId)
                                       .OrderBy(c => c.StopNumber)
                                       .ToListAsync();

                    foreach (var c in customersToShift)
                    {
                        c.StopNumber -= 1;
                    }

                    // Wenn die neue Stopnummer größer ist als die alte, müssen wir sie um 1 verringern, da die Kunden mit höheren Stopnummern bereits um 1 verschoben wurden.
                    if (newStopNumber > oldStopNumber)
                    {
                        return newStopNumber - 1;
                    }
                }
            }
            return -1;
        }
        [HttpDelete("deleteCustomer/{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            if (id <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Id." });
         
            try
            {
                var existingCustomer = await _context.Customers.FindAsync(id);
                if (existingCustomer == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Kunde nicht gefunden." });
                }

                // bevor löschen,save die stopnumber und DistributionLine shift wenn nötig
                int savedDistributionLineId = existingCustomer.DistributionLineId;
                int savedStopNumber = existingCustomer.StopNumber;

                _context.Customers.Remove(existingCustomer);

               
                    await ShiftStopNumbersAsync(savedDistributionLineId, 0,savedStopNumber,id, isDelete: true);

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
