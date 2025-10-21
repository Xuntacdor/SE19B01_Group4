using System;
using System.Collections.Generic;

namespace WebAPI.Models;

public partial class Tag
{
    public int TagId { get; set; }

    public string TagName { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<Post> Posts { get; set; } = new List<Post>();
}
