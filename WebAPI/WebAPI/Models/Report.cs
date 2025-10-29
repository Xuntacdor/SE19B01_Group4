using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Report
{
    public int ReportId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public int? CommentId { get; set; }
    
    public int? CommentAuthorUserId { get; set; } 

    public virtual Comment Comment { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
