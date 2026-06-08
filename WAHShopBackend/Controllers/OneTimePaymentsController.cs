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
        public async Task<IActionResult> Add(OneTimePayment oneTimePayment)
        {
            if (oneTimePayment == null)
                return BadRequest(new ValidationResult() { Result = false, Message = "Die Daten für die Einmalzahlung dürfen nicht null sein.." });
            try
            {
                var targetPickupDate = oneTimePayment.PickupDate.Date;


                // Überprüfen, ob bereits eine Einmalzahlung für denselben Kunden, dieselbe Verteilungslinie und dasselbe PickupDate existiert
                bool isDuplicate = await _context.OneTimePayments.AnyAsync(p =>
                    p.CustomerId == oneTimePayment.CustomerId &&
                    p.DistributionLineId == oneTimePayment.DistributionLineId &&
                    p.PickupDate.Date == targetPickupDate);

                if (isDuplicate)
                {
                    return BadRequest(new ValidationResult()
                    {
                        Result = false,
                        Message = "Für diesen Kunden existiert bereits eine Einmalzahlung in diesem Datum, Sie können eine Zahlung auf der Einmalzahlung Seite bearbeiten. / يوجد بالفعل دفعة لهذا الزبون في هاذا اليوم , يمكنك تعديل عملية الدفع في صفحة الدفع Einmalzahlung."
                    });
                }

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

                // wenn caretAt null ist, dann holen wir die von Database, damit nicht fehler entsteht, da CreatedAt in der Datenbank automatisch generiert,
                // manchmal wird in Forntend neu Payment erstellt, und in gleiche Setzung den Payment updatet wird, da die CratedAt null, und noch nicht von Database geholt
                if (editOneTimePayment.CreatedAt == null)
                    editOneTimePayment.CreatedAt = existingPayment.CreatedAt; // CreatedAt nicht aktualisieren

                _context.Entry(existingPayment).CurrentValues.SetValues(editOneTimePayment);
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
                return BadRequest(new ValidationResult() { Result = false, Message = "Ungültige ID für die Einmalzahlung." });
            try
            {
                var existingPayment = await _context.OneTimePayments.FindAsync(id);
                if (existingPayment == null)
                    return NotFound(new ValidationResult() { Result = false, Message = "Einmalzahlung nicht gefunden." });
                _context.OneTimePayments.Remove(existingPayment);
                int result = await _context.SaveChangesAsync();
                if (result <= 0)
                    return StatusCode(500, new ValidationResult() { Result = false, Message = "Fehler beim Löschen der Einmalzahlung." });

                return Ok(new ValidationResult() { Result = true, Message = "Einmalzahlung erfolgreich gelöscht." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"Fehler beim Löschen der Einmalzahlung: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }
    }
}
