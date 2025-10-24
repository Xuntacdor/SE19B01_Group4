using System;

namespace WebAPI.Models
{
    public partial class PostAttachment
    {
        public int AttachmentId { get; set; }
        public int PostId { get; set; }
        public string FileName { get; set; } = null!;
        public string FileUrl { get; set; } = null!;  // Cloudinary URL
        public string FileType { get; set; } = null!;  // e.g., "image", "document", "audio"
        public string FileExtension { get; set; } = null!; // e.g., "jpg", "pdf", "mp3"
        public long FileSize { get; set; }  // File size in bytes
        public DateTime CreatedAt { get; set; }

        public virtual Post Post { get; set; } = null!;
    }
}
