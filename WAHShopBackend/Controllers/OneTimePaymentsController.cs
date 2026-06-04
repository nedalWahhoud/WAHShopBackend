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
        private const int GroupingPeriod = 2;
        private readonly MyDbContext _context = context;
        [HttpPost("add")]
        public async Task<IActionResult> Add(OneTimePayment oneTimePayment)
        {
            if (oneTimePayment == null)
                return BadRequest(new ValidationResult() { Result = false, Message = "Die Daten für die Einmalzahlung dürfen nicht null sein.." });
            try
            {
                var paymentDate = (oneTimePayment.CreatedAt ?? DateTime.Now).Date;

                // حساب حدود الفترة الزمنية (يومين قبل ويومين بعد) لمنع التداخل في التجميع
                var startDate = paymentDate.AddDays(-GroupingPeriod);
                var endDate = paymentDate.AddDays(GroupingPeriod);

                // فحص قاعدة البيانات: هل هناك دفعة لنفس الزبون ونفس الخط في هذه الفترة؟
                bool isDuplicate = await _context.OneTimePayments.AnyAsync(p =>
                    p.CustomerId == oneTimePayment.CustomerId &&
                    p.DistributionLineId == oneTimePayment.DistributionLineId &&
                    p.CreatedAt.HasValue &&
                    p.CreatedAt.Value.Date >= startDate &&
                    p.CreatedAt.Value.Date <= endDate);

                if (isDuplicate)
                {
                    return BadRequest(new ValidationResult()
                    {
                        Result = false,
                        Message = "Für diesen Kunden existiert bereits eine Einmalzahlung in diesem Zeitraum, Sie können eine Zahlung auf der Einmalzahlung Seite bearbeiten. / يوجد بالفعل دفعة لهذا الزبون خلال هذه الفترة, يمكنك تعديل عملية الدفع في صفحة الدفع Einmalzahlung."
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
                var allPayments = await _context.OneTimePayments
                    .Include(p => p.Customer)
                    .Where(p => p.DistributionLineId == lineId && p.CreatedAt.HasValue)
                    .OrderBy(p => p.Customer != null ? p.Customer.StopNumber : 0)
                    .ToListAsync();

                if (allPayments == null || allPayments.Count == 0)
                    return NotFound(new ValidationResult() { Result = true, Message = "Keine Einmalzahlungen für die angegebene Verteilungslinie gefunden." });

                var groupedResults = new List<OneTimePaymentsGroupDto>();
                OneTimePaymentsGroupDto currentGroup = null!;

                foreach (var payment in allPayments)
                {
                    DateTime paymentDate = payment.CreatedAt!.Value.Date;

                    if (currentGroup == null || paymentDate >= currentGroup.GroupStartDate.AddDays(GroupingPeriod))
                    {
                        currentGroup = new OneTimePaymentsGroupDto
                        {
                            GroupStartDate = paymentDate,
                            Payments = new List<OneTimePayment>()
                        };
                        groupedResults.Add(currentGroup);
                    }

                    currentGroup.Payments.Add(payment);
                }


                return Ok(groupedResults);
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
