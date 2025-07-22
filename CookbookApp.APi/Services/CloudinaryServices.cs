// Location: Services/CloudinaryService.cs

using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CookbookApp.APi.Models;
using Microsoft.Extensions.Options;

namespace CookbookApp.APi.Services
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IOptions<CloudinarySettings> config)
        {
            // This constructor is correct and securely reads from your appsettings.json
            var account = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(account);
        }

        /// <summary>
        /// Uploads a file to Cloudinary and returns the complete result.
        /// The caller is responsible for checking the result for errors and getting the URL/PublicId.
        /// </summary>
        public async Task<ImageUploadResult> UploadImageAsync(IFormFile file)
        {
            var uploadResult = new ImageUploadResult();

            if (file.Length > 0)
            {
                await using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(file.FileName, stream),
                    Folder = "Cookbook" // Organizes all uploads into a "Cookbook" folder in Cloudinary
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }

            return uploadResult;
        }

        /// <summary>
        /// Deletes a file from Cloudinary. Requires the PublicId from the upload result.
        /// </summary>
        public async Task<DeletionResult> DeleteImageAsync(string publicId)
        {
            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);
            return result;
        }
    }
}