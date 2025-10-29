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

namespace WebAPI.Tests.Unit.ExternalService
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

        // ✅ Basic constructor test
        [Fact]
        public void Constructor_ShouldInitialize_WithValidConfiguration()
        {
            var service = new CloudinaryService(_configMock.Object);
            service.Should().NotBeNull();
        }

        // ✅ Upload Image
        [Fact]
        public void Upload_ImageUploadParams_ShouldReturnResult()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake image content"));
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription("test.jpg", stream),
                Folder = "test-folder"
            };

            var result = _service.Upload(uploadParams);

            result.Should().NotBeNull();
        }

        [Fact]
        public void Upload_ImageUploadParams_ShouldThrowException_WhenParamsIsNull()
        {
            Action act = () => _service.Upload(null);
            act.Should().Throw<ArgumentNullException>();
        }

        // ✅ Upload Raw
        [Fact]
        public void Upload_RawUploadParams_ShouldReturnResult()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("fake document content"));
            var uploadParams = new RawUploadParams
            {
                File = new FileDescription("test.pdf", stream),
                Folder = "test-folder"
            };

            var result = _service.Upload(uploadParams);
            result.Should().NotBeNull();
        }

        [Fact]
        public void Upload_RawUploadParams_ShouldThrowException_WhenParamsIsNull()
        {
            Action act = () => _service.Upload((RawUploadParams)null);
            act.Should().Throw<ArgumentNullException>();
        }

 
        // ✅ Edge case: empty stream
        [Fact]
        public void Upload_ShouldHandleEmptyStream()
        {
            var emptyStream = new MemoryStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription("empty.jpg", emptyStream)
            };

            var result = _service.Upload(uploadParams);
            result.Should().NotBeNull();
        }

        // ✅ Edge case: special filename
        [Fact]
        public void Upload_ShouldHandleSpecialCharactersInFilename()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription("tệp có dấu tiếng việt.jpg", stream)
            };

            var result = _service.Upload(uploadParams);
            result.Should().NotBeNull();
        }
    }
}
