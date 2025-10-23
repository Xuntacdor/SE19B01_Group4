using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController : ControllerBase
    {
        private readonly Cloudinary _cloudinary;

        public UploadController(IConfiguration config)
        {
            var acc = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
        }

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        public IActionResult UploadImage([FromForm] UploadFileDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded.");

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(dto.File.FileName, dto.File.OpenReadStream()),
                Folder = "ieltsphobic/images",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = _cloudinary.Upload(uploadParams);
            return Ok(new { url = result.SecureUrl.ToString() });
        }

        [HttpPost("audio")]
        [Consumes("multipart/form-data")]
        public IActionResult UploadAudio([FromForm] UploadFileDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No audio uploaded.");

            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(dto.File.FileName, dto.File.OpenReadStream()),
                Folder = "ieltsphobic/audio",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false,
            };

            var result = _cloudinary.Upload(uploadParams, "video");

            if (result.Error != null)
            {
                Console.WriteLine($"Cloudinary Error: {result.Error.Message}");
                return BadRequest($"Cloudinary error: {result.Error.Message}");
            }

            if (result.SecureUrl == null)
                return BadRequest("Audio upload failed. Cloudinary did not return a URL.");

            return Ok(new { url = result.SecureUrl.ToString() });
        }

    }
}
