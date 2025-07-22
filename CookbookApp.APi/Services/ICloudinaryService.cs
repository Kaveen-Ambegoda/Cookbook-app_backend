// Location: Services/ICloudinaryService.cs

using CloudinaryDotNet.Actions;
using System.Threading.Tasks;

namespace CookbookApp.APi.Services
{
    public interface ICloudinaryService
    {
        /// <summary>
        /// Uploads an image file to Cloudinary.
        /// </summary>
        /// <param name="file">The IFormFile to upload.</param>
        /// <returns>The full upload result from Cloudinary, which includes the PublicId and SecureUrl.</returns>
        Task<ImageUploadResult> UploadImageAsync(IFormFile file);

        /// <summary>
        /// Deletes an image from Cloudinary using its Public ID.
        /// </summary>
        /// <param name="publicId">The Public ID of the image to delete.</param>
        /// <returns>The deletion result from Cloudinary.</returns>
        Task<DeletionResult> DeleteImageAsync(string publicId);
    }
}