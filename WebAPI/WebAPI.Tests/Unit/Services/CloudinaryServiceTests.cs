using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.IO;
using System.Text;
using WebAPI.ExternalServices;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Services
{
    public class CloudinaryServiceTests
    {
        private readonly Mock<IConfiguration> _configMock;
        private readonly CloudinaryService _service;

        public CloudinaryServiceTests()
        {
            _configMock = new Mock<IConfiguration>();
            _configMock.Setup(c => c["Cloudinary:CloudName"]).Returns("test-cloud");
            _configMock.Setup(c => c["Cloudinary:ApiKey"]).Returns("test-key");
            _configMock.Setup(c => c["Cloudinary:ApiSecret"]).Returns("test-secret");

            _service = new CloudinaryService(_configMock.Object);
        }

        // -------------------------------
        // Constructor Tests
        // -------------------------------
        [Fact]
        public void Constructor_ShouldInitialize_WithValidConfiguration()
        {
            // Arrange & Act
            var service = new CloudinaryService(_configMock.Object);

            // Assert
            service.Should().NotBeNull();
            _configMock.Verify(c => c["Cloudinary:CloudName"], Times.Once);
            _configMock.Verify(c => c["Cloudinary:ApiKey"], Times.Once);
            _configMock.Verify(c => c["Cloudinary:ApiSecret"], Times.Once);
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenCloudNameIsNull()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["Cloudinary:CloudName"]).Returns((string)null);
            config.Setup(c => c["Cloudinary:ApiKey"]).Returns("key");
            config.Setup(c => c["Cloudinary:ApiSecret"]).Returns("secret");

            // Act & Assert
            var act = () => new CloudinaryService(config.Object);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenApiKeyIsNull()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["Cloudinary:CloudName"]).Returns("cloud");
            config.Setup(c => c["Cloudinary:ApiKey"]).Returns((string)null);
            config.Setup(c => c["Cloudinary:ApiSecret"]).Returns("secret");

            // Act & Assert
            var act = () => new CloudinaryService(config.Object);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_ShouldThrowException_WhenApiSecretIsNull()
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["Cloudinary:CloudName"]).Returns("cloud");
            config.Setup(c => c["Cloudinary:ApiKey"]).Returns("key");
            config.Setup(c => c["Cloudinary:ApiSecret"]).Returns((string)null);

            // Act & Assert
            var act = () => new CloudinaryService(config.Object);
            act.Should().Throw<ArgumentException>();
        }

        // -------------------------------
        // Upload ImageUploadParams Tests
        // -------------------------------
        [Fact]
        public void Upload_ImageUploadParams_ShouldReturnResult()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake image content"));
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription("test.jpg", stream),
                Folder = "test-folder"
            };

            // Act
            var result = _service.Upload(uploadParams);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<ImageUploadResult>();
        }

        [Fact]
        public void Upload_ImageUploadParams_ShouldThrowException_WhenParamsIsNull()
        {
            // Act & Assert
            var act = () => _service.Upload((ImageUploadParams)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Upload_ImageUploadParams_ShouldHandleError_WhenUploadFails()
        {
            // Arrange
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription("invalid.jpg", Stream.Null)
            };

            // Act
            var result = _service.Upload(uploadParams);

            // Assert
            result.Should().NotBeNull();
            // Cloudinary sẽ trả về error trong result.Error
        }

        // -------------------------------
        // Upload VideoUploadParams Tests
        // -------------------------------
        [Fact]
        public void Upload_VideoUploadParams_ShouldReturnResult()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake video content"));
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription("test.mp4", stream),
                Folder = "test-folder"
            };

            // Act
            var result = _service.Upload(uploadParams, "video");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<VideoUploadResult>();
        }

        [Fact]
        public void Upload_VideoUploadParams_ShouldThrowException_WhenParamsIsNull()
        {
            // Act & Assert
            var act = () => _service.Upload((VideoUploadParams)null, "video");
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Upload_VideoUploadParams_ShouldThrowException_WhenResourceTypeIsNull()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake content"));
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription("test.mp4", stream)
            };

            // Act & Assert
            var act = () => _service.Upload(uploadParams, null);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Upload_VideoUploadParams_ShouldHandleError_WhenUploadFails()
        {
            // Arrange
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription("invalid.mp4", Stream.Null)
            };

            // Act
            var result = _service.Upload(uploadParams, "video");

            // Assert
            result.Should().NotBeNull();
            // Cloudinary sẽ trả về error trong result.Error
        }

        // -------------------------------
        // Upload RawUploadParams Tests
        // -------------------------------
        [Fact]
        public void Upload_RawUploadParams_ShouldReturnResult()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake document content"));
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription("test.pdf", stream),
                Folder = "test-folder"
            };

            // Act
            var result = _service.Upload(uploadParams);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<RawUploadResult>();
        }

        [Fact]
        public void Upload_RawUploadParams_ShouldThrowException_WhenParamsIsNull()
        {
            // Act & Assert
            var act = () => _service.Upload((RawUploadParams)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Upload_RawUploadParams_ShouldHandleError_WhenUploadFails()
        {
            // Arrange
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription("invalid.pdf", Stream.Null)
            };

            // Act
            var result = _service.Upload(uploadParams);

            // Assert
            result.Should().NotBeNull();
            // Cloudinary sẽ trả về error trong result.Error
        }

        // -------------------------------
        // Integration-like Tests
        // -------------------------------
        [Fact]
        public void Upload_Image_ShouldSetCorrectProperties()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription("photo.jpg", stream),
                Folder = "ieltsphobic/images",
                UseFilename = true,
                UniqueFilename = true,
                Overwrite = false
            };

            // Act
            var result = _service.Upload(uploadParams);

            // Assert
            result.Should().NotBeNull();
            uploadParams.Folder.Should().Be("ieltsphobic/images");
            uploadParams.UseFilename.Should().BeTrue();
            uploadParams.UniqueFilename.Should().BeTrue();
            uploadParams.Overwrite.Should().BeFalse();
        }

        [Fact]
        public void Upload_Audio_ShouldUseVideoResourceType()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("audio content"));
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription("audio.mp3", stream),
                Folder = "ieltsphobic/audio"
            };

            // Act
            var result = _service.Upload(uploadParams, "video");

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<VideoUploadResult>();
        }

        [Fact]
        public void Upload_Document_ShouldHandleDifferentFileTypes()
        {
            // Arrange
            var pdfStream = new MemoryStream(Encoding.UTF8.GetBytes("pdf content"));
            var docStream = new MemoryStream(Encoding.UTF8.GetBytes("doc content"));

            var pdfParams = new RawUploadParams
            {
                File = new FileDescription("document.pdf", pdfStream),
                Folder = "ieltsphobic/documents"
            };

            var docParams = new RawUploadParams
            {
                File = new FileDescription("document.docx", docStream),
                Folder = "ieltsphobic/documents"
            };

            // Act
            var pdfResult = _service.Upload(pdfParams);
            var docResult = _service.Upload(docParams);

            // Assert
            pdfResult.Should().NotBeNull();
            docResult.Should().NotBeNull();
        }

        // -------------------------------
        // Edge Cases
        // -------------------------------
        [Fact]
        public void Upload_ShouldHandleEmptyStream()
        {
            // Arrange
            var emptyStream = new MemoryStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription("empty.jpg", emptyStream)
            };

            // Act
            var result = _service.Upload(uploadParams);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Upload_ShouldHandleLargeFiles()
        {
            // Arrange
            var largeContent = new byte[5 * 1024 * 1024]; // 5MB
            var stream = new MemoryStream(largeContent);
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription("large.pdf", stream)
            };

            // Act
            var result = _service.Upload(uploadParams);

            // Assert
            result.Should().NotBeNull();
        }

        [Fact]
        public void Upload_ShouldHandleSpecialCharactersInFilename()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription("tệp có dấu tiếng việt.jpg", stream)
            };

            // Act
            var result = _service.Upload(uploadParams);

            // Assert
            result.Should().NotBeNull();
        }

        // -------------------------------
        // Configuration Tests
        // -------------------------------
        [Theory]
        [InlineData("", "key", "secret")]
        [InlineData("cloud", "", "secret")]
        [InlineData("cloud", "key", "")]
        public void Constructor_ShouldThrowException_WhenConfigIsEmpty(string cloud, string key, string secret)
        {
            // Arrange
            var config = new Mock<IConfiguration>();
            config.Setup(c => c["Cloudinary:CloudName"]).Returns(cloud);
            config.Setup(c => c["Cloudinary:ApiKey"]).Returns(key);
            config.Setup(c => c["Cloudinary:ApiSecret"]).Returns(secret);

            // Act & Assert
            var act = () => new CloudinaryService(config.Object);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Service_ShouldReadConfiguration_FromCorrectKeys()
        {
            // Arrange & Act
            var service = new CloudinaryService(_configMock.Object);

            // Assert
            _configMock.Verify(c => c["Cloudinary:CloudName"], Times.Once);
            _configMock.Verify(c => c["Cloudinary:ApiKey"], Times.Once);
            _configMock.Verify(c => c["Cloudinary:ApiSecret"], Times.Once);
        }
    }
}