using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SuppliersController(MyDbContext context) : ControllerBase
    {
        public readonly MyDbContext _context = context;
        [HttpGet("getSuppliers")]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var manufacturers = await _context.Suppliers
                    .AsNoTracking()
                    .ToListAsync();

                var getItems = new GetItems<Suppliers>
                {
                    Items = manufacturers,
                    AllItemsLoaded = true
                };

                return Ok(getItems);
            }
            catch
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error" });
            }
        }
        [HttpPost("addSupplier")]
        public async Task<IActionResult> AddSupplier(Suppliers supplier)
        {
            if (supplier == null)
            {
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Lieferantendaten" });
            }

            try
            {
                _context.Suppliers.Add(supplier);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    return Ok(new ValidationResult { Result = true, NewId = supplier.Id });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Fehler beim Speichern des Lieferanten" });
                }
            }
            catch
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = "Internal server error" });
            }

        }
        [HttpPut("updateSupplier")]
        public async Task<IActionResult> UpdateSupplier(Suppliers supplier)
        {
            if (supplier == null || supplier.Id <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Lieferantendaten" });
            try
            {
                var existingSupplier = await _context.Suppliers.FindAsync(supplier.Id);
                if (existingSupplier == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Lieferant nicht gefunden." });
                /* existingSupplier.Name = supplier.Name;
                 existingSupplier.Street = supplier.Street;
                 existingSupplier.HNumber = supplier.HNumber;
                 existingSupplier.PostalCode = supplier.PostalCode;
                 existingSupplier.City = supplier.City;
                 existingSupplier.Country = supplier.Country;
                 existingSupplier.Phone = supplier.Phone;
                 existingSupplier.Email = supplier.Email;
                 existingSupplier.Website = supplier.Website;*/
                _context.Entry(existingSupplier).CurrentValues.SetValues(supplier);

                if (_context.ChangeTracker.HasChanges())
                {
                    int result = await _context.SaveChangesAsync();
                    if (result > 0)
                        return Ok(new ValidationResult { Result = true, Message = "Lieferant erfolgreich aktualisiert." });
                    else
                        return StatusCode(500, new ValidationResult { Result = false, Message = "Fehler beim Aktualisieren des Lieferanten" }); await _context.SaveChangesAsync();
                }
                else
                    return Ok(new ValidationResult { Result = true, Message = "Keine Änderungen am Lieferanten festgestellt." });
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
        [HttpDelete("deleteSupplier/{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {


            if (id <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Lieferant-Id" });

            try
            {
                var supplier = await _context.Suppliers.FindAsync(id);
                if (supplier == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Supplier nicht gefunden." });

                _context.Suppliers.Remove(supplier);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                    return Ok(new ValidationResult { Result = true, Message = "Supplier erfolgreich gelöscht." });
                else
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Fehler bei löschen des Lieferant" });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
