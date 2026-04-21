using Microsoft.AspNetCore.Mvc;
using WAHShopBackend.Data;
using Microsoft.EntityFrameworkCore;
using WAHShopBackend.Models;
using WAHShopBackend.ImagesF;

namespace WAHShopBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CarouselController(MyDbContext context,CarouselImagesService carouselImagesService) : ControllerBase
    {
        private readonly MyDbContext _context = context;
        private readonly CarouselImagesService _carouselImagesService = carouselImagesService;
        [HttpGet("getAllCarouselImages")]
        public async Task<IActionResult> GetCarouselImages()
        {
            try
            {
                var currentDate = DateTime.Now;
                var carouselImages = await _context.CarouselImage
                    .Where(ci => ci.StartDate <= currentDate && ci.EndDate >= currentDate)
                    .OrderBy(ci => ci.DisplayOrder)
                    .ToListAsync();
                if (carouselImages == null || carouselImages.Count == 0)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "No carousel images found." });
                }
                return Ok(carouselImages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpGet("getCarouselImageById/{id}")]
        public async Task<IActionResult> GetCarouselImageById(int id)
        {
            try
            {
                var carouselImage = await _context.CarouselImage.FindAsync(id);
                if (carouselImage == null)
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Carousel image nicht gefunden." });
                }
                return Ok(carouselImage);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPost("addCarouselImage")]
        public async Task<IActionResult> AddCarouselImage([FromBody] CarouselImage carouselImage)
        {
            if (carouselImage == null || carouselImage.ImageBytes == null || carouselImage.ImageBytes.Length == 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Karussellbilddaten." });
            try
            {
                _context.CarouselImage.Add(carouselImage);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    if(carouselImage.ImageBytes != null && carouselImage.ImageBytes.Length > 0)
                    {
                        var resultImage = _carouselImagesService.AddImage(carouselImage);
                        if (resultImage.Result == true)
                        {
                            int result1 = await _context.SaveChangesAsync();
                            if (result1 == 0)
                            {
                                await DeleteCarouselImage(carouselImage.Id);
                                _carouselImagesService.DeleteImage(carouselImage.Id);
                                return StatusCode(500, new ValidationResult { Result = false, Message = "Das carouselImage wurde hinzugefügt (aber wieder gelöscht), das Bild konnte nicht in der Datenbank gespeichert werden." });
                            }
                            return Ok(new ValidationResult { Result = true, NewId = carouselImage.Id});
                        }
                        else
                        {
                            await DeleteCarouselImage(carouselImage.Id);
                            return StatusCode(500, new ValidationResult { Result = false, Message = "Das carouselImage wurde hinzugefügt (aber wieder gelöscht), das Bild konnte nicht gespeichert werden: " + resultImage.Message });
                        }
                    }
                    else
                    {
                        await DeleteCarouselImage(carouselImage.Id);
                        return BadRequest(new ValidationResult { Result = false, Message = "Keine Bilddaten bereitgestellt." });
                    }
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Fehler beim Hinzufügen des Carousel images." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }
        [HttpPut("updateCarouselImage")]
        public async Task<IActionResult> UpdateCarouselImage([FromBody] CarouselImage editCarouselImage)
        {
            if (editCarouselImage == null || editCarouselImage.Id <= 0)
                return BadRequest(new ValidationResult { Result = false, Message = "Ungültige Karussellbilddaten." });
            try
            {
                var existingCarouselImage = await _context.CarouselImage.FindAsync(editCarouselImage.Id);
                if (existingCarouselImage != null)
                {
                    var resultImage = _carouselImagesService.EditImage(existingCarouselImage, editCarouselImage);
                    if (resultImage.Result == false)
                    {
                        return StatusCode(500, new ValidationResult { Result = false, Message = "Bildaktualisierung fehlgeschlagen: " + resultImage.Message });
                    }
                    //
                    _context.Entry(existingCarouselImage).CurrentValues.SetValues(editCarouselImage);
                    _context.Entry(existingCarouselImage).State = EntityState.Modified;
                    int result = await _context.SaveChangesAsync();
                    if (result > 0)
                    {
                        return Ok(new ValidationResult { Result = true, Message = "CarouselImage erfolgreich aktualisiert" });
                    }
                    else
                    {
                        return StatusCode(500, new ValidationResult { Result = false, Message = "Produktaktualisierung fehlgeschlagen" });
                    }
                }
                else
                {
                    return NotFound(new ValidationResult { Result = false, Message = "Carousel image nicht gefunden." });
                }
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
        [HttpDelete("deleteCarouselImage/{id}")]
        public async Task<IActionResult> DeleteCarouselImage(int id)
        {
            try
            {
                var carouselImage = await _context.CarouselImage.FindAsync(id);
                if (carouselImage == null)
                    return NotFound(new ValidationResult { Result = false, Message = "Carousel image nicht gefunden." });

                _context.CarouselImage.Remove(carouselImage);
                int result = await _context.SaveChangesAsync();
                if (result > 0)
                {
                    _carouselImagesService.DeleteImage(id);
                    return Ok(new ValidationResult { Result = true, Message = "Carousel image erfolgreich gelöscht." });
                }
                else
                {
                    return StatusCode(500, new ValidationResult { Result = false, Message = "Fehler beim Löschen des Carousel images." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ValidationResult { Result = false, Message = ex.Message });
            }
        }

    }
}
