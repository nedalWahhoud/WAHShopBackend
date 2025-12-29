using WAHShopBackend.Models;

namespace WAHShopBackend.ImagesF
{
    public class CarouselImagesService(AppConfig appConfig)
    {
        private readonly AppConfig _appConfig = appConfig;
        public ValidationResult AddImage(CarouselImage carouselImage)
        {

            try
            {
                // create catousel images folder if not exists
                string folderPath = Path.Combine(_appConfig.ShareStoragePath, "CarouselImages");
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }
                // image bytes schreiben
                string imagePath = Path.Combine(folderPath, $"{carouselImage.Id}.jpg");
                File.WriteAllBytes(imagePath, carouselImage.ImageBytes!);

                string shareStoragePath = _appConfig.ShareStoragePath;
                string relativePath = Path.GetRelativePath(shareStoragePath, imagePath).Replace("\\", "/");

                carouselImage.ImageUrl = relativePath;
                return new ValidationResult() { Result = true, Message = relativePath };
            }
            catch (Exception ex)
            {
                return new ValidationResult() { Result = false, Message = ex.Message };
            }
        }
        public ValidationResult EditImage(CarouselImage currentCarouselImage, CarouselImage editCarouselImage)
        {
            try
            {
                // prüfen ob bild geändert wurde
                if (currentCarouselImage != null && editCarouselImage != null && currentCarouselImage.LastModified != editCarouselImage.LastModified)
                {
                    if (editCarouselImage.ImageBytes == null || editCarouselImage.ImageBytes.Length == 0)
                    {
                        return new ValidationResult() { Result = false, Message = "Kein image data bereitgestellt." };
                    }

                    string folderPath = Path.Combine(_appConfig.ShareStoragePath, "CarouselImages");
                    if (Directory.Exists(folderPath))
                    {
                        string imagePath = Path.Combine(folderPath, $"{editCarouselImage.Id}.jpg");

                        if (File.Exists(imagePath))
                        {
                            File.Delete(imagePath);
                        }

                        File.WriteAllBytes(imagePath, editCarouselImage.ImageBytes!);

                        string shareStoragePath = _appConfig.ShareStoragePath;
                        string relativePath = Path.GetRelativePath(shareStoragePath, imagePath).Replace("\\", "/");

                        editCarouselImage.ImageUrl = relativePath;
                        // update die LastModified in der currentProductImage, um in database zu aktualisieren
                        currentCarouselImage.LastModified = editCarouselImage.LastModified;
                        return new ValidationResult() { Result = true, Message = relativePath };
                    }
                    else
                    {
                        return new ValidationResult() { Result = false, Message = "Image Folder nicht gefunden." };
                    }
                }
                else
                {
                    return new ValidationResult() { Result = true, Message = "keine Änderungen entdeckt." };
                }
            }
            catch (Exception ex)
            {
                return new ValidationResult() { Result = false, Message = ex.Message };
            }
        }
        public ValidationResult DeleteImage(int id)
        {
            try
            {
                string folderPath = Path.Combine(_appConfig.ShareStoragePath, "CarouselImages");
                string imagePath = Path.Combine(folderPath, $"{id}.jpg");
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                }
                return new ValidationResult() { Result = true, Message = "Image deleted successfully." };
            }
            catch (Exception ex)
            {
                return new ValidationResult() { Result = false, Message = ex.Message };
            }
        }
    }
}
