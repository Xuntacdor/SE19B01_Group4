using System.ComponentModel.DataAnnotations;

namespace WebAPI.DTOs
{
    public class PostDTO
    {
        public int PostId { get; set; }
        public string Title { get; set; } = null!;
        public string Content { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int VoteCount { get; set; }
        public bool IsVoted { get; set; }
        public bool IsPinned { get; set; }
        public bool IsHiddenByUser { get; set; }
        public string? RejectionReason { get; set; }
        public UserDTO User { get; set; } = null!;
        public List<TagDTO> Tags { get; set; } = new List<TagDTO>();
        public List<PostAttachmentDTO> Attachments { get; set; } = new List<PostAttachmentDTO>();
    }

    public class CreatePostDTO
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [Required]
        public string Content { get; set; } = null!;

        public List<string> TagNames { get; set; } = new List<string>();
        
        public List<CreatePostAttachmentDTO> Attachments { get; set; } = new List<CreatePostAttachmentDTO>();
    }

    public class UpdatePostDTO
    {
        [StringLength(200)]
        public string? Title { get; set; }

        public string? Content { get; set; }

        public List<string>? TagNames { get; set; }

        public List<CreatePostAttachmentDTO>? Attachments { get; set; }
    }

    public class PostAttachmentDTO
    {
        public int AttachmentId { get; set; }
        public int PostId { get; set; }
        public string FileName { get; set; } = null!;
        public string FileUrl { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public string FileExtension { get; set; } = null!;
        public long FileSize { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePostAttachmentDTO
    {
        [Required]
        public string FileName { get; set; } = null!;
        
        [Required]
        public string FileUrl { get; set; } = null!;
        
        [Required]
        public string FileType { get; set; } = null!;
        
        [Required]
        public string FileExtension { get; set; } = null!;
        
        [Required]
        public long FileSize { get; set; }
    }
}

