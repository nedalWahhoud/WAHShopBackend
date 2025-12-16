using WAHShopBackend.Models;

namespace WAHShopBackend.ProductP
{
    public class ProductService
    {

        public bool IsValidProduct(Product newProduct, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(newProduct.Name_de))
            {
                errorMessage = "Product name is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newProduct.Description_de))
            {
                errorMessage = "Description is required.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(newProduct.Name_ar))
            {
                errorMessage = "Product name is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newProduct.Description_ar))
            {
                errorMessage = "Description is required.";
                return false;
            }

            if (newProduct!.CategoryId <= 0)
            {
                errorMessage = "Category is required.";
                return false;
            }
            if (newProduct.Quantity < 0)
            {
                errorMessage = "Quantity must be greater than -1.";
                return false;
            }
            if (newProduct.SalePrice <= 0)
            {
                errorMessage = "Selling price must be greater than 0.";
                return false;
            }
            if (newProduct.PurchasePrice <= 0)
            {
                errorMessage = "Purchase price must be greater than 0.";
                return false;
            }
            if (newProduct.MinimumStock < 0)
            {
                errorMessage = "Minimum Stock must be greater than -1.";
                return false;
            }
            if (newProduct.ManufacturerId <= 0)
            {
                errorMessage = "manufacturer is required.";
                return false;
            }
            if (newProduct.TaxRateId <= 0)
            {
                errorMessage = "Tax Rate is required.";
                return false;
            }
            if (newProduct.EXPDate < DateTime.Now)
            {
                errorMessage = "Expiration date must be greater than current date.";
                return false;
            }
            foreach (var item in newProduct.ProductImages)
            {

                if (item.ImageBytes == null || (item.ImageBytes != null && item.ImageBytes.Length <= 0))
                {
                    errorMessage = "Image is required.";
                    return false;
                }
            }

            errorMessage = string.Empty;
            return true;
        }
    }
}
