using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using WebAPI.Services;

namespace WebAPI.ExternalServices
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var acc = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
        }

        public ImageUploadResult Upload(ImageUploadParams uploadParams)
        {
            return _cloudinary.Upload(uploadParams);
        }

        public VideoUploadResult Upload(VideoUploadParams uploadParams, string resourceType)
        {
            return (VideoUploadResult)_cloudinary.Upload(uploadParams, resourceType);
        }

        public RawUploadResult Upload(RawUploadParams uploadParams)
        {
            return _cloudinary.Upload(uploadParams);
        }
    }
}