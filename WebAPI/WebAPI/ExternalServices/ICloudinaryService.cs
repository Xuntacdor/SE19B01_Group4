using CloudinaryDotNet.Actions;

namespace WebAPI.Services
{
    public interface ICloudinaryService
    {
        ImageUploadResult Upload(ImageUploadParams uploadParams);
        VideoUploadResult Upload(VideoUploadParams uploadParams, string resourceType);
        RawUploadResult Upload(RawUploadParams uploadParams);
    }
}