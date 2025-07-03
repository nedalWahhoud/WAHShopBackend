using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;


namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AddressesController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        [HttpPost("addAddress")]
        public async Task<IActionResult> AddAddress([FromBody] Address address)
        {
            if (address == null)
                return BadRequest(new ValidationResult { Result = false, Message = "Address cannot be null." });
            try
            {

                _context.Addresses.Add(address);
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Id:{address.Id}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getAddressesByUserId/{UserId}")]
        public async Task<IActionResult> GetAddressesByUserId(int UserId, [FromQuery] List<int>? excludeAddressesIds)
        {
            if (UserId <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Invalid User ID." });
            try
            {
                var addresses = await _context.Addresses
                    .Where(a => a.UserId == UserId &&
                     !excludeAddressesIds!.Contains(a.Id))
                    .ToListAsync();
                if (addresses != null && addresses.Count > 0)
                    return Ok(addresses);
                else
                    return NotFound(new ValidationResult { Result = false, Message = "No addresses found for this user." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpDelete("deleteAddress/{id}")]
        public async Task<IActionResult> DeleteAddress(int id)
        {
            if (id <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Invalid Address ID." });
            try
            {
                var address = await _context.Addresses.FindAsync(id);
                if (address == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Address not found." });
                _context.Addresses.Remove(address);
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Address with ID {id} deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPut("updateAddress")]
        public async Task<IActionResult> UpdateAddress([FromBody] Address address)
        {
            if (address == null || address.Id <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Invalid address data." });
            try
            {
                var existingAddress = await _context.Addresses.FindAsync(address.Id);
                if (existingAddress == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Address not found." });
                existingAddress.FirstName = address.FirstName;
                existingAddress.LastName = address.LastName;
                existingAddress.Phone = address.Phone;
                existingAddress.Street = address.Street;
                existingAddress.ZipCode = address.ZipCode;
                existingAddress.City = address.City;
                existingAddress.Country = address.Country;
                _context.Addresses.Update(existingAddress);
                int result = await _context.SaveChangesAsync();
                return Ok(new ValidationResult { Result = result > 0, Message = $"Address with ID {address.Id} updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
    }
}
