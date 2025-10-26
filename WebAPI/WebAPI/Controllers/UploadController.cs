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


        [HttpPost("document")]
        [Consumes("multipart/form-data")]
        public IActionResult UploadDocument([FromForm] UploadFileDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No document uploaded.");

            // Validate file type
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".xls", ".xlsx", ".ppt", ".pptx" };
            var fileExtension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                return BadRequest($"File type {fileExtension} is not allowed. Allowed types: {string.Join(", ", allowedExtensions)}");

            // Validate file size (10MB limit)
            if (dto.File.Length > 10 * 1024 * 1024)
                return BadRequest("File size must be less than 10MB.");

            var uploadParams = new RawUploadParams
            {
                File = new FileDescription(dto.File.FileName, dto.File.OpenReadStream()),
                Folder = "ieltsphobic/documents",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var result = _cloudinary.Upload(uploadParams);
            return Ok(new
            {
                url = result.SecureUrl.ToString(),
                fileName = dto.File.FileName,
                fileSize = dto.File.Length,
                fileType = fileExtension
            });
        }

        [HttpPost("file")]
        [Consumes("multipart/form-data")]
        public IActionResult UploadFile([FromForm] UploadFileDto dto)
        {
            if (dto.File == null || dto.File.Length == 0)
                return BadRequest("No file uploaded.");

            var fileExtension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
            var fileName = dto.File.FileName;
            var fileSize = dto.File.Length;

            // Determine file type and folder
            string folder;
            if (IsImageFile(fileExtension))
            {
                folder = "ieltsphobic/images";
                var uploadParams = new ImageUploadParams
                {
                    File = new FileDescription(fileName, dto.File.OpenReadStream()),
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                    Overwrite = false
                };
                var result = _cloudinary.Upload(uploadParams);
                
                var secureUrl = result.SecureUrl?.ToString() ?? result.Url?.ToString();
                
                Console.WriteLine($"Uploaded image URL: {secureUrl}");
                
                return Ok(new
                {
                    url = secureUrl,
                    fileName = fileName,
                    fileSize = fileSize,
                    fileType = fileExtension,
                    category = "image"
                });
            }
            else if (IsDocumentFile(fileExtension))
            {
                folder = "ieltsphobic/documents";
                // Validate file size (10MB limit for documents)
                if (fileSize > 10 * 1024 * 1024)
                    return BadRequest("Document file size must be less than 10MB.");
            }
            else if (IsAudioFile(fileExtension))
            {
                folder = "ieltsphobic/audio";
            }
            else
            {
                return BadRequest($"File type {fileExtension} is not supported.");
            }

            var rawUploadParams = new RawUploadParams
            {
                File = new FileDescription(fileName, dto.File.OpenReadStream()),
                Folder = folder,
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            var rawResult = _cloudinary.Upload(rawUploadParams);
            return Ok(new
            {
                url = rawResult.SecureUrl.ToString(),
                fileName = fileName,
                fileSize = fileSize,
                fileType = fileExtension,
                category = IsDocumentFile(fileExtension) ? "document" : "audio"
            });
        }

        private bool IsImageFile(string extension)
        {
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg" };
            return imageExtensions.Contains(extension);
        }

        private bool IsDocumentFile(string extension)
        {
            var documentExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".rtf", ".xls", ".xlsx", ".ppt", ".pptx" };
            return documentExtensions.Contains(extension);
        }

        private bool IsAudioFile(string extension)
        {
            var audioExtensions = new[] { ".mp3", ".wav", ".m4a", ".aac", ".ogg" };
            return audioExtensions.Contains(extension);
        }
    }
}