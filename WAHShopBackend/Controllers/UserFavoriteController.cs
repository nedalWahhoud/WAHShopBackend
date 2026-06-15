using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Data;
using WAHShopBackend.Models;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserFavoriteController(MyDbContext context) : ControllerBase
    {
        private readonly MyDbContext _context = context;

        [HttpPost("add/{userId}/{productId}")]
        public async Task<IActionResult> Add(int userId, int productId)
        {
            if(userId <= 0 || productId <= 0)
                return BadRequest(new ValidationResult() { Result = false, Message = "Die Daten für die Favoriten dürfen nicht null sein.." });
            try
            {
                var alreadyExists = await _context.UserFavorite.AnyAsync(f => f.UserId == userId && f.ProductId == productId);
                if (alreadyExists)
                {
                    return Ok(new ValidationResult() { Result = true, Message = "Produkt ist bereits in der Favoritenliste." });
                }

                var userFavorite = new UserFavorite
                {
                    UserId = userId,
                    ProductId = productId,
                };

                _context.UserFavorite.Add(userFavorite);
                int result = await _context.SaveChangesAsync();
                if (result <= 0)
                    return StatusCode(500, new ValidationResult() { Result = false, Message = "Fehler beim Hinzufügen der Favoriten.." });
                return Ok(new ValidationResult() { Result = true, Message = "Favoriten erfolgreich hinzugefügt.." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"Fehler beim Hinzufügen der Favoriten: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }
        [HttpDelete("delete/{userId}/{productId}")]
        public async Task<IActionResult> Delete(int userId, int productId)
        {
            try
            {
                var favorite = await _context.UserFavorite
                    .FirstOrDefaultAsync(f => f.UserId == userId && f.ProductId == productId);
                if (favorite == null)
                    return NotFound(new ValidationResult() { Result = false, Message = "Favoriten nicht gefunden.." });
                _context.UserFavorite.Remove(favorite);
                int result = await _context.SaveChangesAsync();
                if (result <= 0)
                    return StatusCode(500, new ValidationResult() { Result = false, Message = "Fehler beim Löschen der Favoriten.." });
                return Ok(new ValidationResult() { Result = true, Message = "Favoriten erfolgreich gelöscht.." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult() { Result = false, Message = $"Fehler beim Löschen der Favoriten: {ex.InnerException?.Message ?? ex.Message}" });
            }
        }
        [HttpPost("getFavoritesProducts/{userId}")]
        public async Task<IActionResult> GetFavoritesProductIds(int userId, [FromBody] GetItems<Product> getItems)
        {
            if (userId <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Benutzer-ID." });
            if (getItems == null)
                return BadRequest(new ValidationResult { Result = false, Message = "Fehlerhafte Anfrage-Daten." });

            try
            {
                var query = _context.UserFavorite
                    .AsNoTracking()
                    .Where(p => p.UserId == userId)
                    .Select(p => p.ProductId);

                var ids = await query
                    .Skip(getItems.CurrentPage * getItems.PageSize)
                    .Take(getItems.PageSize)
                    .ToListAsync();

                var totalCount = await query.CountAsync();
                var loadedCount = (getItems.CurrentPage * getItems.PageSize) + ids.Count;
                if (loadedCount >= totalCount)
                {
                    getItems.AllItemsLoaded = true;
                }

                //
                var favoriteProducts = await _context.Products
                    .OrderBy(c => c.Name_de)
                    .Include(p => p.ProductImages)
                    .Where(p => ids.Contains(p.Id))
                    .ToListAsync();

                foreach (var product in favoriteProducts)
                {
                    product.IsFavorite = true;
                }

                getItems.Items = favoriteProducts;



                return Ok(getItems);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}
