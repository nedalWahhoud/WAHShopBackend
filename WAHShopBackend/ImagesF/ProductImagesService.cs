using WAHShopBackend.Models;

namespace WAHShopBackend.ImagesF
{
    public class ProductImagesService(AppConfig appConfig)
    {
        private readonly AppConfig _appConfig = appConfig;
        public ValidationResult AddImage(Product newProduct)
        {
            ValidationResult validationResult = null!;
            foreach (var item in newProduct.ProductImages)
            {
                try
                {
                    string folderPath = Path.Combine(_appConfig.ShareStoragePath, "ProductsImages");
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    string productFolderPath = Path.Combine(folderPath, item.ProductId.ToString());
                    // erstellen product folder 
                    Directory.CreateDirectory(productFolderPath);
                    string imagePath = Path.Combine(productFolderPath, $"{item.ProductId}.jpg");
                    File.WriteAllBytes(imagePath, item.ImageBytes!);

                    string shareStoragePath = _appConfig.ShareStoragePath;
                    //string shareStoragePath = Path.Combine(_env.ContentRootPath, "ShareStorage");
                    string relativePath = Path.GetRelativePath(shareStoragePath, imagePath).Replace("\\", "/");

                    validationResult = new ValidationResult() { Result = true, Message = relativePath };
                    item.ProductId = newProduct.Id;
                    item.ImageUrl = relativePath;
                }
                catch (Exception ex)
                {
                    validationResult = new ValidationResult() { Result = false, Message = ex.Message };
                }
            }
            return validationResult;
        }
        public ValidationResult EditImage(ICollection<ProductImages> currentProductImages, ICollection<ProductImages> editProductImages)
        {
            ValidationResult validationResult = null!;
            foreach (var item in editProductImages)
            {

                var currentProductImage = currentProductImages.FirstOrDefault(c => c.Id == item.Id);
                if (currentProductImage != null && currentProductImage.LastModified != item.LastModified)
                {

                    // prüfen ob bild geändert wurde
                    if (item.ImageBytes == null || item.ImageBytes.Length == 0)
                    {
                        validationResult = new ValidationResult() { Result = false, Message = "No image data provided." };
                        break;
                    }

                    try
                    {
                        string folderPath = Path.Combine(_appConfig.ShareStoragePath, "ProductsImages");
                        string productFolderPath = Path.Combine(folderPath, item.ProductId.ToString());
                        if (Directory.Exists(folderPath) && Directory.Exists(productFolderPath))
                        {
                            string imagePath = Path.Combine(productFolderPath, $"{item.ProductId}.jpg");

                            if (File.Exists(imagePath))
                            {
                                File.Delete(imagePath);
                            }

                            File.WriteAllBytes(imagePath, item.ImageBytes!);

                            string shareStoragePath = _appConfig.ShareStoragePath;
                            string relativePath = Path.GetRelativePath(shareStoragePath, imagePath).Replace("\\", "/");

                            validationResult = new ValidationResult() { Result = true, Message = relativePath };
                            item.ImageUrl = relativePath;
                            // update die LastModified in der currentProductImage, um in database zu aktualisieren
                            currentProductImage.LastModified = item.LastModified;   
                        }
                        else
                        {
                            validationResult = new ValidationResult() { Result = false, Message = "Image Folder nicht gefunden." };
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        validationResult = new ValidationResult() { Result = false, Message = ex.Message };
                        break;
                    }
                }
                else
                {
                    validationResult = new ValidationResult() { Result = true, Message = "keine Änderungen entdeckt." };
                }
            }
            return validationResult;
        }
        public ValidationResult DeleteImage(int Id,bool WithFolder)
        {
            try
            {
                if (WithFolder)
                {
                    string folderPath = Path.Combine(_appConfig.ShareStoragePath, "ProductsImages", Id.ToString());
                    if (Directory.Exists(folderPath))
                    {
                        Directory.Delete(folderPath, true);
                    }
                }
                else
                {
                    string imagePath = Path.Combine(_appConfig.ShareStoragePath, "ProductsImages", Id.ToString(), $"{Id}.jpg");
                    if (File.Exists(imagePath))
                    {
                        File.Delete(imagePath);
                    }
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
