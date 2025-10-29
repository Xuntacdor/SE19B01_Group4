using CloudinaryDotNet.Actions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.IO;
using System.Text;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Controllers
{
    public class UploadControllerTests
    {
        private readonly Mock<ICloudinaryService> _cloudinaryMock;
        private readonly UploadController _controller;

        public UploadControllerTests()
        {
            _cloudinaryMock = new Mock<ICloudinaryService>();
            _controller = new UploadController(_cloudinaryMock.Object);
        }

        private IFormFile CreateFakeFile(string name, string contentType = "text/plain", int size = 1024)
        {
            var bytes = Encoding.UTF8.GetBytes(new string('a', size));
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", name)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private static T GetValue<T>(object obj, string prop)
        {
            return (T)obj.GetType().GetProperty(prop)!.GetValue(obj)!;
        }

        // -------------------------------
        // UploadImage Tests
        // -------------------------------
        [Fact]
        public void UploadImage_ShouldReturnOk_WhenValidFile()
        {
            var file = CreateFakeFile("test.png");
            var dto = new UploadFileDto { File = file };
            _cloudinaryMock.Setup(c => c.Upload(It.IsAny<ImageUploadParams>()))
                .Returns(new ImageUploadResult { SecureUrl = new Uri("http://img.com/test.png") });

            var result = _controller.UploadImage(dto) as OkObjectResult;
            result.Should().NotBeNull();

            var value = result!.Value;
            GetValue<string>(value, "url").Should().Be("http://img.com/test.png");
        }

        [Fact]
        public void UploadImage_ShouldReturnBadRequest_WhenFileMissing()
        {
            var dto = new UploadFileDto { File = null };
            var result = _controller.UploadImage(dto) as BadRequestObjectResult;
            result!.Value.Should().Be("No file uploaded.");
        }

        // -------------------------------
        // UploadAudio Tests
        // -------------------------------
        [Fact]
        public void UploadAudio_ShouldReturnOk_WhenValidFile()
        {
            var file = CreateFakeFile("audio.mp3");
            var dto = new UploadFileDto { File = file };
            _cloudinaryMock.Setup(c => c.Upload(It.IsAny<VideoUploadParams>(), "video"))
                .Returns(new VideoUploadResult { SecureUrl = new Uri("http://audio.com/test.mp3") });

            var result = _controller.UploadAudio(dto) as OkObjectResult;
            var value = result!.Value;
            GetValue<string>(value, "url").Should().Be("http://audio.com/test.mp3");
        }

        [Fact]
        public void UploadAudio_ShouldReturnBadRequest_WhenErrorFromCloudinary()
        {
            var file = CreateFakeFile("bad.mp3");
            var dto = new UploadFileDto { File = file };
            _cloudinaryMock.Setup(c => c.Upload(It.IsAny<VideoUploadParams>(), "video"))
                .Returns(new VideoUploadResult { Error = new Error { Message = "Failed" } });

            var result = _controller.UploadAudio(dto) as BadRequestObjectResult;
            ((string)result!.Value).Should().Contain("Cloudinary error");
        }

        [Fact]
        public void UploadAudio_ShouldReturnBadRequest_WhenNoUrl()
        {
            var file = CreateFakeFile("bad.mp3");
            var dto = new UploadFileDto { File = file };
            _cloudinaryMock.Setup(c => c.Upload(It.IsAny<VideoUploadParams>(), "video"))
                .Returns(new VideoUploadResult());

            var result = _controller.UploadAudio(dto) as BadRequestObjectResult;
            ((string)result!.Value).Should().Contain("Audio upload failed");
        }

        // -------------------------------
        // UploadDocument Tests
        // -------------------------------
        [Fact]
        public void UploadDocument_ShouldReturnOk_WhenValidFile()
        {
            var file = CreateFakeFile("doc.pdf");
            var dto = new UploadFileDto { File = file };
            _cloudinaryMock.Setup(c => c.Upload(It.IsAny<RawUploadParams>()))
                .Returns(new RawUploadResult { SecureUrl = new Uri("http://docs.com/doc.pdf") });

            var result = _controller.UploadDocument(dto) as OkObjectResult;
            var value = result!.Value;
            GetValue<string>(value, "url").Should().Be("http://docs.com/doc.pdf");
        }

        [Fact]
        public void UploadDocument_ShouldReject_InvalidExtension()
        {
            var file = CreateFakeFile("malware.exe");
            var dto = new UploadFileDto { File = file };
            var result = _controller.UploadDocument(dto) as BadRequestObjectResult;
            ((string)result!.Value).Should().Contain(".exe");
        }

        [Fact]
        public void UploadDocument_ShouldReject_LargeFile()
        {
            var file = CreateFakeFile("big.pdf", size: 11 * 1024 * 1024);
            var dto = new UploadFileDto { File = file };
            var result = _controller.UploadDocument(dto) as BadRequestObjectResult;
            ((string)result!.Value).Should().Contain("less than 10MB");
        }

        // -------------------------------
        // UploadFile Tests
        // -------------------------------
        [Fact]
        public void UploadFile_ShouldUpload_Image()
        {
            var file = CreateFakeFile("photo.jpg");
            var dto = new UploadFileDto { File = file };

            _cloudinaryMock.Setup(c => c.Upload(It.IsAny<ImageUploadParams>()))
                .Returns(new ImageUploadResult { SecureUrl = new Uri("http://img.com/photo.jpg") });

            var result = _controller.UploadFile(dto) as OkObjectResult;
            var value = result!.Value;
            GetValue<string>(value, "category").Should().Be("image");
        }

        [Fact]
        public void UploadFile_ShouldUpload_Document()
        {
            var file = CreateFakeFile("report.pdf");
            var dto = new UploadFileDto { File = file };

            _cloudinaryMock.Setup(c => c.Upload(It.IsAny<RawUploadParams>()))
                .Returns(new RawUploadResult { SecureUrl = new Uri("http://docs.com/report.pdf") });

            var result = _controller.UploadFile(dto) as OkObjectResult;
            var value = result!.Value;
            GetValue<string>(value, "category").Should().Be("document");
        }

        [Fact]
        public void UploadFile_ShouldUpload_Audio()
        {
            var file = CreateFakeFile("sound.mp3");
            var dto = new UploadFileDto { File = file };

            _cloudinaryMock.Setup(c => c.Upload(It.IsAny<RawUploadParams>()))
                .Returns(new RawUploadResult { SecureUrl = new Uri("http://audio.com/sound.mp3") });

            var result = _controller.UploadFile(dto) as OkObjectResult;
            var value = result!.Value;
            GetValue<string>(value, "category").Should().Be("audio");
        }

        [Fact]
        public void UploadFile_ShouldReject_TooLargeDocument()
        {
            var file = CreateFakeFile("report.pdf", size: 11 * 1024 * 1024);
            var dto = new UploadFileDto { File = file };

            var result = _controller.UploadFile(dto) as BadRequestObjectResult;
            ((string)result!.Value).Should().Contain("Document file size must be less");
        }

        [Fact]
        public void UploadFile_ShouldReject_UnsupportedExtension()
        {
            var file = CreateFakeFile("weird.xyz");
            var dto = new UploadFileDto { File = file };

            var result = _controller.UploadFile(dto) as BadRequestObjectResult;
            ((string)result!.Value).Should().Contain("not supported");
        }

        [Fact]
        public void UploadFile_ShouldReject_MissingFile()
        {
            var dto = new UploadFileDto { File = null };
            var result = _controller.UploadFile(dto) as BadRequestObjectResult;
            ((string)result!.Value).Should().Contain("No file uploaded");
        }

        // -------------------------------
        // Helpers coverage
        // -------------------------------
        [Fact]
        public void HelperMethods_ShouldReturnTrue_ForValidExtensions()
        {
            var ext = typeof(UploadController)
                .GetMethod("IsImageFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var docExt = typeof(UploadController)
                .GetMethod("IsDocumentFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var audExt = typeof(UploadController)
                .GetMethod("IsAudioFile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            ext!.Invoke(_controller, new object[] { ".jpg" }).Should().Be(true);
            docExt!.Invoke(_controller, new object[] { ".pdf" }).Should().Be(true);
            audExt!.Invoke(_controller, new object[] { ".mp3" }).Should().Be(true);
        }
        [Fact]
        public void UploadAudio_ShouldReturnBadRequest_WhenFileMissing()
        {
            var dto = new UploadFileDto { File = null };

            var result = _controller.UploadAudio(dto) as BadRequestObjectResult;

            result.Should().NotBeNull();
            result!.Value.Should().Be("No audio uploaded.");
        }

        // -------------------------------
        // UploadDocument Negative Test
        // -------------------------------
        [Fact]
        public void UploadDocument_ShouldReturnBadRequest_WhenFileMissing()
        {
            var dto = new UploadFileDto { File = null };

            var result = _controller.UploadDocument(dto) as BadRequestObjectResult;

            result.Should().NotBeNull();
            result!.Value.Should().Be("No document uploaded.");
        }
    }
}
