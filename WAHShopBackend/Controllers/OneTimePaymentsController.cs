using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class OneTimePaymentsController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] OneTimePayment oneTimePayment)
        {
            if (oneTimePayment == null)
                return BadRequest(new ValidationResult() { Result = false, Message = "Die Daten für die Einmalzahlung dürfen nicht null sein.." });
            try
            {
                var startOfDay = oneTimePayment.PickupDate.Date;
                var endOfDay = startOfDay.AddDays(1);

                // Überprüfen, ob bereits eine Einmalzahlung für denselben Kunden, dieselbe Verteilungslinie und dasselbe PickupDate existiert
                bool isDuplicate = await _context.OneTimePayments.AnyAsync(p =>
                    p.CustomerId == oneTimePayment.CustomerId &&
                    p.DistributionLineId == oneTimePayment.DistributionLineId &&
                    p.PickupDate >= startOfDay &&
                    p.PickupDate < endOfDay);

                if (isDuplicate)
                {
                    return BadRequest(new ValidationResult()
                    {
                        Result = false,
                        Message = "Für diesen Kunden existiert bereits eine Einmalzahlung in diesem Datum, Sie können eine Zahlung auf der Einmalzahlung Seite bearbeiten. / يوجد بالفعل دفعة لهذا الزبون في هاذا اليوم , يمكنك تعديل عملية الدفع في صفحة الدفع Einmalzahlung."
                    });
                }

                // Alte Zahlungen im Hintergrund löschen
                await DeleteOldPaymentsAsync();
                // add
                _context.OneTimePayments.Add(oneTimePayment);
                int result = await _context.SaveChangesAsync();
                if (result <= 0)
                    return StatusCode(500, new ValidationResult() { Result = false, Message = "Fehler beim Hinzufügen der Einmalzahlung.." });

                return Ok(new ValidationResult() { Result = true, Message = "Einmalzahlung erfolgreich hinzugefügt..",NewId = oneTimePayment.Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"Fehler beim Hinzufügen der Einmalzahlung: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }
        private static DateTime? _lastCleanupDate = null;
        private async Task DeleteOldPaymentsAsync()
        {
            DateTime tody = DateTime.Now.Date;

            if(tody == _lastCleanupDate)
            {
                // Bereits heute aufgeräumt, keine Aktion erforderlich
                return;
            }

            try
            {
                // Berechne das Datum von vor einem Monat.
                var oneMonthAgo = DateTime.Now.AddMonths(-1);

                // Löschen Sie Zahlungen direkt aus der Datenbank 
                await _context.OneTimePayments
                    .Where(p => p.PickupDate < oneMonthAgo)
                    .ExecuteDeleteAsync();

                // Aktualisiere das letzte Aufräumdatum
                _lastCleanupDate = tody;

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fehler beim automatischen Löschen: {ex.Message}");
            }
        }
        // wir nehmen die OneTimeZahlungen immer Tag der created und ein Tag danach, da die Zahlungen immer vortag der verteilung erstellt werden oder am selben Tag.
        [HttpGet("getGroupedPaymentsByLineId/{lineId}")]
        public async Task<IActionResult> GetGroupedPaymentsByLineId(int lineId)
        {
            try
            {
                var groupedData = await _context.OneTimePayments
            .Include(p => p.Customer)
            .Where(p => p.DistributionLineId == lineId)
            .GroupBy(p => p.PickupDate.Date) // gruppieren nach Datum (nur Tag, Monat, Jahr)
            .Select(g => new OneTimePaymentsGroupDto
            {
                GroupPickupDate = g.Key,
                // inner sortieren nach StopNumber, damit die Zahlungen in der Reihenfolge der Kundenstops innerhalb der Gruppe angezeigt werden
                Payments = g.OrderBy(p => p.Customer != null ? p.Customer.StopNumber : 0)
                            .ToList()
            })
            .OrderBy(g => g.GroupPickupDate) // die Gruppen selbst nach Datum sortieren
            .ToListAsync();

                if (groupedData == null || groupedData.Count == 0)
                    return NotFound(new ValidationResult() { Result = true, Message = "Keine Einmalzahlungen für die angegebene Verteilungslinie gefunden." });

                return Ok(groupedData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"Fehler beim Abrufen der Einmalzahlungen: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }
        [HttpPut("updateStauts")]
        public async Task<IActionResult> UpdateStauts([FromBody] OneTimePayment editOneTimePayment)
        {
            try
            {
                var existingPayment = await _context.OneTimePayments.FindAsync(editOneTimePayment.Id);
                if (existingPayment == null)
                    return NotFound(new ValidationResult() { Result = false, Message = "Einmalzahlung nicht gefunden." });

                _context.Entry(existingPayment).CurrentValues.SetValues(editOneTimePayment);
                // CreatedAt nicht aktualisieren
                _context.Entry(existingPayment).Property(x => x.CreatedAt).IsModified = false;

                int result = await _context.SaveChangesAsync();
                if (result <= 0)
                    return StatusCode(500, new ValidationResult() { Result = false, Message = "Fehler beim Aktualisieren des Status der Einmalzahlung." });
                return Ok(new ValidationResult() { Result = true, Message = "Status der Einmalzahlung erfolgreich aktualisiert." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"Fehler beim Aktualisieren des Status der Einmalzahlung: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest(new ValidationResult() { Result = false, Message = "Ungültige ID." });
            try
            {
                int rowsAffected = await _context.OneTimePayments
                                   .Where(p => p.Id == id)
                                   .ExecuteDeleteAsync();

                if (rowsAffected == 0)
                    return NotFound(new ValidationResult() { Result = false, Message = "Einmalzahlung nicht gefunden." });

                return Ok(new ValidationResult() { Result = true, Message = "Einmalzahlung erfolgreich gelöscht." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"Fehler beim Löschen der Einmalzahlung: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }
    }
}
