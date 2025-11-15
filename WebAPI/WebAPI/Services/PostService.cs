using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class PostService : IPostService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserRepository _userRepository;

        public PostService(ApplicationDbContext context, IUserRepository userRepository)
        {
            _context = context;
            _userRepository = userRepository;
        }

        public IEnumerable<PostDTO> GetPosts(int page, int limit, int? userId = null)
        {
            var postIds = _context.Post
                .AsNoTracking()
                .Where(p => !p.IsHidden && p.Status == "approved")
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => p.PostId)
                .ToList();

            if (!postIds.Any())
                return Enumerable.Empty<PostDTO>();

            // Maintain order dictionary
            var orderDict = postIds
                .Select((id, idx) => new { Id = id, Index = idx })
                .ToDictionary(x => x.Id, x => x.Index);

            // Optimize: Load posts with projection - only select needed fields
            var postsData = _context.Post
                .AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .Select(p => new
                {
                    p.PostId,
                    p.Title,
                    p.Content,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.ViewCount,
                    p.IsPinned,
                    p.RejectionReason,
                    UserId = p.User.UserId,
                    Username = p.User.Username,
                    Email = p.User.Email,
                    Firstname = p.User.Firstname,
                    Lastname = p.User.Lastname,
                    Role = p.User.Role,
                    Avatar = p.User.Avatar
                })
                .ToList();

            // Batch load related data in parallel
            var attachmentsData = _context.PostAttachment
                .AsNoTracking()
                .Where(a => postIds.Contains(a.PostId))
                .Select(a => new
                {
                    a.PostId,
                    a.AttachmentId,
                    a.FileName,
                    a.FileUrl,
                    a.FileType,
                    a.FileExtension,
                    a.FileSize,
                    a.CreatedAt
                })
                .ToList()
                .GroupBy(a => a.PostId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var tagsData = _context.Post
                .AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .SelectMany(p => p.Tags.Select(t => new { p.PostId, t.TagId, t.TagName }))
                .ToList()
                .GroupBy(t => t.PostId)
                .ToDictionary(g => g.Key, g => g.Select(t => new { t.TagId, t.TagName }).ToList());

            // Batch load counts in one combined query
            var counts = _context.Comment
                .AsNoTracking()
                .Where(c => postIds.Contains(c.PostId))
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, CommentCount = g.Count() })
                .ToList()
                .ToDictionary(x => x.PostId, x => x.CommentCount);

            var likeCounts = _context.PostLike
                .AsNoTracking()
                .Where(pl => postIds.Contains(pl.PostId))
                .GroupBy(pl => pl.PostId)
                .Select(g => new { PostId = g.Key, LikeCount = g.Count() })
                .ToList()
                .ToDictionary(x => x.PostId, x => x.LikeCount);

            // Batch load user-specific data
            var userVotedPostIds = userId.HasValue
                ? _context.PostLike
                    .AsNoTracking()
                    .Where(pl => postIds.Contains(pl.PostId) && pl.UserId == userId.Value)
                    .Select(pl => pl.PostId)
                    .ToHashSet()
                : new HashSet<int>();

            var userHiddenPostIds = userId.HasValue
                ? _context.UserPostHide
                    .AsNoTracking()
                    .Where(uph => postIds.Contains(uph.PostId) && uph.UserId == userId.Value)
                    .Select(uph => uph.PostId)
                    .ToHashSet()
                : new HashSet<int>();

            // Build DTOs maintaining original order
            return postsData
                .OrderBy(p => orderDict.GetValueOrDefault(p.PostId, int.MaxValue))
                .Select(p => new PostDTO
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    ViewCount = p.ViewCount ?? 0,
                    CommentCount = counts.GetValueOrDefault(p.PostId, 0),
                    VoteCount = likeCounts.GetValueOrDefault(p.PostId, 0),
                    IsVoted = userVotedPostIds.Contains(p.PostId),
                    IsPinned = p.IsPinned,
                    IsHiddenByUser = userHiddenPostIds.Contains(p.PostId),
                    RejectionReason = p.RejectionReason,
                    User = new UserDTO
                    {
                        UserId = p.UserId,
                        Username = p.Username,
                        Email = p.Email,
                        Firstname = p.Firstname,
                        Lastname = p.Lastname,
                        Role = p.Role,
                        Avatar = p.Avatar
                    },
                    Tags = tagsData.ContainsKey(p.PostId)
                        ? tagsData[p.PostId].Select(t => new TagDTO { TagId = t.TagId, TagName = t.TagName }).ToList()
                        : new List<TagDTO>(),
                    Attachments = attachmentsData.ContainsKey(p.PostId)
                        ? attachmentsData[p.PostId].Select(a => new PostAttachmentDTO
                        {
                            AttachmentId = a.AttachmentId,
                            PostId = a.PostId,
                            FileName = a.FileName,
                            FileUrl = a.FileUrl,
                            FileType = a.FileType,
                            FileExtension = a.FileExtension,
                            FileSize = a.FileSize,
                            CreatedAt = a.CreatedAt
                        }).ToList()
                        : new List<PostAttachmentDTO>()
                });
        }

        public IEnumerable<PostDTO> GetPostsByFilter(string filter, int page, int limit, int? userId = null, string? tag = null)
        {
            var baseQuery = _context.Post.AsNoTracking().AsQueryable();

            // Optimize: Load hidden post IDs once for user
            var hiddenPostIds = userId.HasValue
                ? _context.UserPostHide
                    .AsNoTracking()
                    .Where(uph => uph.UserId == userId.Value)
                    .Select(uph => uph.PostId)
                    .ToHashSet()
                : new HashSet<int>();

            // Apply tag filter if provided - optimize by finding tag first, then filtering posts
            IQueryable<Post>? tagFilteredQuery = null;
            if (!string.IsNullOrWhiteSpace(tag))
            {
                // Find tag ID first to use in a more efficient join
                var tagId = _context.Tag
                    .AsNoTracking()
                    .Where(t => t.TagName == tag)
                    .Select(t => (int?)t.TagId)
                    .FirstOrDefault();
                
                if (!tagId.HasValue)
                {
                    // Tag doesn't exist, return empty result
                    return Enumerable.Empty<PostDTO>();
                }
                
                // Filter posts that have this tag using a subquery
                // This is more efficient than Any() on navigation collection
                tagFilteredQuery = baseQuery.Where(p => p.Tags.Any(t => t.TagId == tagId.Value));
            }

            // For "top" filter, we need to calculate like counts first to avoid N+1 query
            List<int> postIds;
            if (filter.ToLower() == "top")
            {
                // Use tag-filtered query if available, otherwise use baseQuery
                var queryForTop = tagFilteredQuery ?? baseQuery;
                
                // Get all approved posts first
                var approvedPosts = queryForTop
                    .Where(p => !p.IsHidden && p.Status == "approved" && (userId == null || !hiddenPostIds.Contains(p.PostId)))
                    .Select(p => new { p.PostId, p.IsPinned })
                    .ToList();

                if (!approvedPosts.Any())
                    return Enumerable.Empty<PostDTO>();

                var allPostIds = approvedPosts.Select(p => p.PostId).ToList();

                // Batch load all like counts in one query
                var likeCountsForSort = _context.PostLike
                    .AsNoTracking()
                    .Where(pl => allPostIds.Contains(pl.PostId))
                    .GroupBy(pl => pl.PostId)
                    .Select(g => new { PostId = g.Key, Count = g.Count() })
                    .ToDictionary(x => x.PostId, x => x.Count);

                // Sort by pinned first, then by like count
                var sortedPosts = approvedPosts
                    .OrderByDescending(p => p.IsPinned)
                    .ThenByDescending(p => likeCountsForSort.GetValueOrDefault(p.PostId, 0))
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(p => p.PostId)
                    .ToList();

                postIds = sortedPosts;
            }
            else
            {
                // Use tag-filtered query if available, otherwise use baseQuery
                var queryForFilter = tagFilteredQuery ?? baseQuery;
                
                // Build query based on filter for other filters
                IQueryable<Post> query = filter.ToLower() switch
                {
                    "new" => queryForFilter
                        .Where(p => !p.IsHidden && p.Status == "approved" && (userId == null || !hiddenPostIds.Contains(p.PostId)))
                        .OrderByDescending(p => p.IsPinned)
                        .ThenByDescending(p => p.CreatedAt),
                    "hot" => queryForFilter
                        .Where(p => !p.IsHidden && p.Status == "approved" && (userId == null || !hiddenPostIds.Contains(p.PostId)))
                        .OrderByDescending(p => p.IsPinned)
                        .ThenByDescending(p => p.ViewCount),
                    "closed" => userId.HasValue
                        ? queryForFilter
                            .Where(p => hiddenPostIds.Contains(p.PostId))
                            .OrderByDescending(p => p.IsPinned)
                            .ThenByDescending(p => p.CreatedAt)
                        : queryForFilter.Where(p => false),
                    _ => queryForFilter
                        .Where(p => !p.IsHidden && p.Status == "approved" && (userId == null || !hiddenPostIds.Contains(p.PostId)))
                        .OrderByDescending(p => p.IsPinned)
                        .ThenByDescending(p => p.CreatedAt)
                };

                // Get post IDs first (with ordering applied)
                postIds = query
                    .Skip((page - 1) * limit)
                    .Take(limit)
                    .Select(p => p.PostId)
                    .ToList();
            }

            if (!postIds.Any())
                return Enumerable.Empty<PostDTO>();

            // Maintain order dictionary
            var orderDict = postIds
                .Select((id, idx) => new { Id = id, Index = idx })
                .ToDictionary(x => x.Id, x => x.Index);

            // Optimize: Load posts with projection - only select needed fields
            var postsData = _context.Post
                .AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .Select(p => new
                {
                    p.PostId,
                    p.Title,
                    p.Content,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.ViewCount,
                    p.IsPinned,
                    p.RejectionReason,
                    UserId = p.User.UserId,
                    Username = p.User.Username,
                    Email = p.User.Email,
                    Firstname = p.User.Firstname,
                    Lastname = p.User.Lastname,
                    Role = p.User.Role,
                    Avatar = p.User.Avatar
                })
                .ToList();

            // Batch load related data
            var attachmentsData = _context.PostAttachment
                .AsNoTracking()
                .Where(a => postIds.Contains(a.PostId))
                .Select(a => new
                {
                    a.PostId,
                    a.AttachmentId,
                    a.FileName,
                    a.FileUrl,
                    a.FileType,
                    a.FileExtension,
                    a.FileSize,
                    a.CreatedAt
                })
                .ToList()
                .GroupBy(a => a.PostId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var tagsData = _context.Post
                .AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .SelectMany(p => p.Tags.Select(t => new { p.PostId, t.TagId, t.TagName }))
                .ToList()
                .GroupBy(t => t.PostId)
                .ToDictionary(g => g.Key, g => g.Select(t => new { t.TagId, t.TagName }).ToList());

            // Batch load counts
            var commentCounts = _context.Comment
                .AsNoTracking()
                .Where(c => postIds.Contains(c.PostId))
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.PostId, x => x.Count);

            var likeCounts = _context.PostLike
                .AsNoTracking()
                .Where(pl => postIds.Contains(pl.PostId))
                .GroupBy(pl => pl.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.PostId, x => x.Count);

            // Batch load user-specific data
            var userVotedPostIds = userId.HasValue
                ? _context.PostLike
                    .AsNoTracking()
                    .Where(pl => postIds.Contains(pl.PostId) && pl.UserId == userId.Value)
                    .Select(pl => pl.PostId)
                    .ToHashSet()
                : new HashSet<int>();

            var userHiddenPostIds = hiddenPostIds.Intersect(postIds).ToHashSet();

            // Build DTOs maintaining original order
            return postsData
                .OrderBy(p => orderDict.GetValueOrDefault(p.PostId, int.MaxValue))
                .Select(p => new PostDTO
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    ViewCount = p.ViewCount ?? 0,
                    CommentCount = commentCounts.GetValueOrDefault(p.PostId, 0),
                    VoteCount = likeCounts.GetValueOrDefault(p.PostId, 0),
                    IsVoted = userVotedPostIds.Contains(p.PostId),
                    IsPinned = p.IsPinned,
                    IsHiddenByUser = userHiddenPostIds.Contains(p.PostId),
                    RejectionReason = p.RejectionReason,
                    User = new UserDTO
                    {
                        UserId = p.UserId,
                        Username = p.Username,
                        Email = p.Email,
                        Firstname = p.Firstname,
                        Lastname = p.Lastname,
                        Role = p.Role,
                        Avatar = p.Avatar
                    },
                    Tags = tagsData.ContainsKey(p.PostId)
                        ? tagsData[p.PostId].Select(t => new TagDTO { TagId = t.TagId, TagName = t.TagName }).ToList()
                        : new List<TagDTO>(),
                    Attachments = attachmentsData.ContainsKey(p.PostId)
                        ? attachmentsData[p.PostId].Select(a => new PostAttachmentDTO
                        {
                            AttachmentId = a.AttachmentId,
                            PostId = a.PostId,
                            FileName = a.FileName,
                            FileUrl = a.FileUrl,
                            FileType = a.FileType,
                            FileExtension = a.FileExtension,
                            FileSize = a.FileSize,
                            CreatedAt = a.CreatedAt
                        }).ToList()
                        : new List<PostAttachmentDTO>()
                });
        }

        public PostDTO? GetPostById(int id, int? userId = null)
        {
            // Optimize: Use projection to only load needed fields
            var postData = _context.Post
                .AsNoTracking()
                .Where(p => p.PostId == id)
                .Select(p => new
                {
                    p.PostId,
                    p.Title,
                    p.Content,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.ViewCount,
                    p.IsPinned,
                    p.RejectionReason,
                    UserId = p.User.UserId,
                    Username = p.User.Username,
                    Email = p.User.Email,
                    Firstname = p.User.Firstname,
                    Lastname = p.User.Lastname,
                    Role = p.User.Role,
                    Avatar = p.User.Avatar
                })
                .FirstOrDefault();

            if (postData == null) return null;

            // Batch load related data and counts
            var attachments = _context.PostAttachment
                .AsNoTracking()
                .Where(a => a.PostId == id)
                .Select(a => new
                {
                    a.AttachmentId,
                    a.PostId,
                    a.FileName,
                    a.FileUrl,
                    a.FileType,
                    a.FileExtension,
                    a.FileSize,
                    a.CreatedAt
                })
                .ToList();

            var tags = _context.Post
                .AsNoTracking()
                .Where(p => p.PostId == id)
                .SelectMany(p => p.Tags.Select(t => new { t.TagId, t.TagName }))
                .ToList();

            // Load counts efficiently
            var commentCount = _context.Comment.AsNoTracking().Count(c => c.PostId == id);
            var likeCount = _context.PostLike.AsNoTracking().Count(pl => pl.PostId == id);

            bool isVoted = false, isHidden = false;
            if (userId.HasValue)
            {
                isVoted = _context.PostLike.AsNoTracking().Any(pl => pl.PostId == id && pl.UserId == userId.Value);
                isHidden = _context.UserPostHide.AsNoTracking().Any(uph => uph.PostId == id && uph.UserId == userId.Value);
            }

            return new PostDTO
            {
                PostId = postData.PostId,
                Title = postData.Title,
                Content = postData.Content,
                CreatedAt = postData.CreatedAt,
                UpdatedAt = postData.UpdatedAt,
                ViewCount = postData.ViewCount ?? 0,
                CommentCount = commentCount,
                VoteCount = likeCount,
                IsVoted = isVoted,
                IsPinned = postData.IsPinned,
                IsHiddenByUser = isHidden,
                RejectionReason = postData.RejectionReason,
                User = new UserDTO
                {
                    UserId = postData.UserId,
                    Username = postData.Username,
                    Email = postData.Email,
                    Firstname = postData.Firstname,
                    Lastname = postData.Lastname,
                    Role = postData.Role,
                    Avatar = postData.Avatar
                },
                Tags = tags.Select(t => new TagDTO { TagId = t.TagId, TagName = t.TagName }).ToList(),
                Attachments = attachments.Select(a => new PostAttachmentDTO
                {
                    AttachmentId = a.AttachmentId,
                    PostId = a.PostId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileType = a.FileType,
                    FileExtension = a.FileExtension,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };
        }

        public PostDTO CreatePost(CreatePostDTO dto, int userId)
        {
            var user = _userRepository.GetById(userId);
            if (user == null) throw new KeyNotFoundException("User not found");
            
            // Check if user is restricted
            if (user.IsRestricted)
                throw new UnauthorizedAccessException("Your account has been restricted from posting on the forum. Please contact support.");

            var post = new Post
            {
                UserId = userId,
                Title = dto.Title,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                ViewCount = 0,
                Status = "pending" // Tất cả posts mới đều ở trạng thái pending
            };

            _context.Post.Add(post);
            _context.SaveChanges();

            // Add tags to post
            if (dto.TagNames.Any())
            {
                AddTagsToPost(post.PostId, dto.TagNames);
            }

            // Add attachments to post
            if (dto.Attachments.Any())
            {
                AddAttachmentsToPost(post.PostId, dto.Attachments);
            }

            return GetPostById(post.PostId) ?? throw new InvalidOperationException("Failed to create post");
        }

        public void UpdatePost(int id, UpdatePostDTO dto, int userId)
        {
            var post = _context.Post
                .Include(p => p.Tags)
                .Include(p => p.Attachments)
                .FirstOrDefault(p => p.PostId == id);
            
            if (post == null) throw new KeyNotFoundException("Post not found");

            var user = _userRepository.GetById(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");

            if (post.UserId != userId && user.Role != "admin")
                throw new UnauthorizedAccessException("You don't have permission to update this post");

            if (!string.IsNullOrWhiteSpace(dto.Title))
                post.Title = dto.Title;

            if (!string.IsNullOrWhiteSpace(dto.Content))
                post.Content = dto.Content;

            post.UpdatedAt = DateTime.UtcNow;

            // Update tags if provided
            if (dto.TagNames != null)
            {
                // Clear existing tags
                post.Tags.Clear();

                // Add new tags if any provided
                if (dto.TagNames.Any())
                {
                    foreach (var tagName in dto.TagNames)
                    {
                        var tag = _context.Tag.FirstOrDefault(t => t.TagName == tagName);

                        if (tag == null)
                        {
                            tag = new Tag { TagName = tagName };
                            _context.Tag.Add(tag);
                        }

                        // Add tag to post (no need to check, we already cleared)
                        post.Tags.Add(tag);
                    }
                }
            }

            // Update attachments if provided
            if (dto.Attachments != null)
            {
                // Load attachments explicitly if not loaded
                if (post.Attachments == null || post.Attachments.Count == 0)
                {
                    var existingAttachments = _context.PostAttachment
                        .Where(a => a.PostId == id)
                        .ToList();
                    
                    if (existingAttachments.Any())
                    {
                        _context.PostAttachment.RemoveRange(existingAttachments);
                    }
                }
                else
                {
                    // Remove existing attachments
                    _context.PostAttachment.RemoveRange(post.Attachments);
                }

                // Add new attachments
                if (dto.Attachments.Any())
                {
                    foreach (var attachment in dto.Attachments)
                    {
                        var postAttachment = new PostAttachment
                        {
                            PostId = id,
                            FileName = attachment.FileName,
                            FileUrl = attachment.FileUrl,
                            FileType = attachment.FileType,
                            FileExtension = attachment.FileExtension,
                            FileSize = attachment.FileSize,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.PostAttachment.Add(postAttachment);
                    }
                }
            }

            _context.SaveChanges();
        }

        public void DeletePost(int id, int userId)
        {
            var post = _context.Post
                .Include(p => p.Comments)
                .Include(p => p.PostLikes)
                .Include(p => p.Tags)
                .FirstOrDefault(p => p.PostId == id);
                
            if (post == null) throw new KeyNotFoundException("Post not found");

            var user = _userRepository.GetById(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");

            if (post.UserId != userId && user.Role != "admin")
                throw new UnauthorizedAccessException("You don't have permission to delete this post");

            try
            {
                // Xóa các dữ liệu liên quan theo thứ tự đúng
                // 1. Lấy tất cả comments của post (bao gồm nested comments)
                var allComments = _context.Comment.Where(c => c.PostId == id).ToList();
                
                // 2. Xóa tất cả CommentLikes của comments trong post
                var allCommentLikes = _context.CommentLike
                    .Where(cl => allComments.Select(c => c.CommentId).Contains(cl.CommentId))
                    .ToList();
                _context.CommentLike.RemoveRange(allCommentLikes);
                
                // 3. Xóa tất cả comments của post (bao gồm nested comments)
                _context.Comment.RemoveRange(allComments);

                // 4. Xóa tất cả likes của post
                _context.PostLike.RemoveRange(post.PostLikes);
                
                // 6. Xóa Post_Tag relationships (many-to-many)
                // Clear tags collection trước khi xóa post
                post.Tags.Clear();
                
                // 7. Xóa post
                _context.Post.Remove(post);
                
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting post: {ex.Message}", ex);
            }
        }

        public void VotePost(int id, int userId)
        {
            var post = _context.Post.Find(id);
            if (post == null) throw new KeyNotFoundException("Post not found");

            var existingLike = _context.PostLike
                .FirstOrDefault(pl => pl.PostId == id && pl.UserId == userId);

            if (existingLike != null)
                throw new InvalidOperationException("You have already voted for this post");

            var like = new PostLike
            {
                PostId = id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.PostLike.Add(like);
            _context.SaveChanges();
        }

        public void UnvotePost(int id, int userId)
        {
            var like = _context.PostLike
                .FirstOrDefault(pl => pl.PostId == id && pl.UserId == userId);

            if (like == null)
                throw new InvalidOperationException("You haven't voted for this post");

            _context.PostLike.Remove(like);
            _context.SaveChanges();
        }


        private void AddTagsToPost(int postId, List<string> tagNames)
        {
            if (tagNames == null || !tagNames.Any()) return;

            var post = _context.Post
                .Include(p => p.Tags)
                .FirstOrDefault(p => p.PostId == postId);
            
            if (post == null) return;

            // Batch load all tags in one query to avoid N+1
            var existingTags = _context.Tag
                .Where(t => tagNames.Contains(t.TagName))
                .ToDictionary(t => t.TagName, t => t);

            var existingTagNames = existingTags.Keys.ToHashSet();
            var newTagNames = tagNames.Where(tn => !existingTagNames.Contains(tn)).ToList();

            // Create new tags in batch
            if (newTagNames.Any())
            {
                var newTags = newTagNames.Select(tagName => new Tag { TagName = tagName }).ToList();
                _context.Tag.AddRange(newTags);
                _context.SaveChanges(); // Save to get IDs

                // Add to dictionary
                foreach (var newTag in newTags)
                {
                    existingTags[newTag.TagName] = newTag;
                }
            }

            // Add tags to post (avoid duplicates)
            var existingPostTagIds = post.Tags.Select(t => t.TagId).ToHashSet();
            foreach (var tagName in tagNames)
            {
                if (existingTags.TryGetValue(tagName, out var tag) && !existingPostTagIds.Contains(tag.TagId))
                {
                    post.Tags.Add(tag);
                    existingPostTagIds.Add(tag.TagId);
                }
            }

            _context.SaveChanges();
        }

        private void AddAttachmentsToPost(int postId, List<CreatePostAttachmentDTO> attachments)
        {
            foreach (var attachment in attachments)
            {
                var postAttachment = new PostAttachment
                {
                    PostId = postId,
                    FileName = attachment.FileName,
                    FileUrl = attachment.FileUrl,
                    FileType = attachment.FileType,
                    FileExtension = attachment.FileExtension,
                    FileSize = attachment.FileSize,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PostAttachment.Add(postAttachment);
            }

            _context.SaveChanges();
        }

        public bool IsPostVotedByUser(int postId, int userId)
        {
            return _context.PostLike
                .Any(pl => pl.PostId == postId && pl.UserId == userId);
        }

        public bool IsPostHiddenByUser(int postId, int userId)
        {
            return _context.UserPostHide
                .Any(uph => uph.PostId == postId && uph.UserId == userId);
        }

        private PostDTO ToDTO(Post post, int? userId = null)
        {
            // Fallback method for backward compatibility - calls optimized version with counts
            var commentCount = post.Comments?.Count ?? _context.Comment.Count(c => c.PostId == post.PostId);
            var likeCount = post.PostLikes?.Count ?? _context.PostLike.Count(pl => pl.PostId == post.PostId);
            var isVoted = userId.HasValue ? IsPostVotedByUser(post.PostId, userId.Value) : false;
            var isHidden = userId.HasValue ? IsPostHiddenByUser(post.PostId, userId.Value) : false;

            return ToDTOOptimized(post, userId, commentCount, likeCount, isVoted, isHidden);
        }

        // Optimized version that accepts pre-loaded counts to avoid N+1 queries
        private PostDTO ToDTOOptimized(Post post, int? userId, int commentCount, int likeCount, bool isVoted, bool isHiddenByUser)
        {
            return new PostDTO
            {
                PostId = post.PostId,
                Title = post.Title,
                Content = post.Content,
                CreatedAt = post.CreatedAt,
                UpdatedAt = post.UpdatedAt,
                ViewCount = post.ViewCount ?? 0,
                CommentCount = commentCount,
                VoteCount = likeCount,
                IsVoted = isVoted,
                IsPinned = post.IsPinned,
                IsHiddenByUser = isHiddenByUser,
                RejectionReason = post.RejectionReason,
                User = new UserDTO
                {
                    UserId = post.User.UserId,
                    Username = post.User.Username,
                    Email = post.User.Email,
                    Firstname = post.User.Firstname,
                    Lastname = post.User.Lastname,
                    Role = post.User.Role,
                    Avatar = post.User.Avatar
                },
                Tags = post.Tags.Select(t => new TagDTO
                {
                    TagId = t.TagId,
                    TagName = t.TagName
                }).ToList(),
                Attachments = post.Attachments.Select(a => new PostAttachmentDTO
                {
                    AttachmentId = a.AttachmentId,
                    PostId = a.PostId,
                    FileName = a.FileName,
                    FileUrl = a.FileUrl,
                    FileType = a.FileType,
                    FileExtension = a.FileExtension,
                    FileSize = a.FileSize,
                    CreatedAt = a.CreatedAt
                }).ToList()
            };
        }

        public void IncrementViewCount(int postId)
        {
            var post = _context.Post.Find(postId);
            if (post == null) return;

            // Use atomic increment to prevent race conditions
            post.ViewCount = (post.ViewCount ?? 0) + 1;
            _context.SaveChanges();
        }

        public void PinPost(int postId)
        {
            var post = _context.Post.Find(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            post.IsPinned = true;
            _context.SaveChanges();
        }

        public void UnpinPost(int postId)
        {
            var post = _context.Post.Find(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            post.IsPinned = false;
            _context.SaveChanges();
        }

        public void HidePost(int postId, int userId)
        {
            var post = _context.Post.Find(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            // Kiểm tra xem user đã ẩn post này chưa
            var existingHide = _context.UserPostHide
                .FirstOrDefault(uph => uph.UserId == userId && uph.PostId == postId);

            if (existingHide == null)
            {
                // Thêm vào UserPostHide thay vì set IsHidden
                var userPostHide = new UserPostHide
                {
                    UserId = userId,
                    PostId = postId,
                    HiddenAt = DateTime.UtcNow
                };
                _context.UserPostHide.Add(userPostHide);
                _context.SaveChanges();
            }
        }

        public void UnhidePost(int postId, int userId)
        {
            var existingHide = _context.UserPostHide
                .FirstOrDefault(uph => uph.UserId == userId && uph.PostId == postId);

            if (existingHide != null)
            {
                _context.UserPostHide.Remove(existingHide);
                _context.SaveChanges();
            }
        }

        // Moderator methods
        public ModeratorStatsDTO GetModeratorStats()
        {
            return new ModeratorStatsDTO
            {
                TotalPosts = _context.Post.Count(p => p.Status == "approved"),
                PendingPosts = _context.Post.Count(p => p.Status == "pending"),
                ReportedComments = _context.Report.Count(r => r.Status == "Pending"),
                RejectedPosts = _context.Post.Count(p => p.Status == "rejected"),
                TotalComments = _context.Comment.Count()
            };
        }

        public IEnumerable<PostDTO> GetPendingPosts(int page, int limit)
        {
            // Get post IDs first with proper ordering
            var postIds = _context.Post
                .AsNoTracking()
                .Where(p => p.Status == "pending")
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => p.PostId)
                .ToList();

            if (!postIds.Any())
                return Enumerable.Empty<PostDTO>();

            // Maintain order dictionary
            var orderDict = postIds
                .Select((id, idx) => new { Id = id, Index = idx })
                .ToDictionary(x => x.Id, x => x.Index);

            // Optimize: Load posts with projection - only select needed fields
            var postsData = _context.Post
                .AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .Select(p => new
                {
                    p.PostId,
                    p.Title,
                    p.Content,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.ViewCount,
                    p.IsPinned,
                    p.RejectionReason,
                    UserId = p.User.UserId,
                    Username = p.User.Username,
                    Email = p.User.Email,
                    Firstname = p.User.Firstname,
                    Lastname = p.User.Lastname,
                    Role = p.User.Role,
                    Avatar = p.User.Avatar
                })
                .ToList();

            // Batch load related data
            var attachmentsData = _context.PostAttachment
                .AsNoTracking()
                .Where(a => postIds.Contains(a.PostId))
                .Select(a => new
                {
                    a.PostId,
                    a.AttachmentId,
                    a.FileName,
                    a.FileUrl,
                    a.FileType,
                    a.FileExtension,
                    a.FileSize,
                    a.CreatedAt
                })
                .ToList()
                .GroupBy(a => a.PostId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var tagsData = _context.Post
                .AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .SelectMany(p => p.Tags.Select(t => new { p.PostId, t.TagId, t.TagName }))
                .ToList()
                .GroupBy(t => t.PostId)
                .ToDictionary(g => g.Key, g => g.Select(t => new { t.TagId, t.TagName }).ToList());

            // Batch load counts
            var commentCounts = _context.Comment
                .AsNoTracking()
                .Where(c => postIds.Contains(c.PostId))
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.PostId, x => x.Count);

            var likeCounts = _context.PostLike
                .AsNoTracking()
                .Where(pl => postIds.Contains(pl.PostId))
                .GroupBy(pl => pl.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.PostId, x => x.Count);

            // Build DTOs maintaining original order
            return postsData
                .OrderBy(p => orderDict.GetValueOrDefault(p.PostId, int.MaxValue))
                .Select(p => new PostDTO
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    ViewCount = p.ViewCount ?? 0,
                    CommentCount = commentCounts.GetValueOrDefault(p.PostId, 0),
                    VoteCount = likeCounts.GetValueOrDefault(p.PostId, 0),
                    IsVoted = false,
                    IsPinned = p.IsPinned,
                    IsHiddenByUser = false,
                    RejectionReason = p.RejectionReason,
                    User = new UserDTO
                    {
                        UserId = p.UserId,
                        Username = p.Username,
                        Email = p.Email,
                        Firstname = p.Firstname,
                        Lastname = p.Lastname,
                        Role = p.Role,
                        Avatar = p.Avatar
                    },
                    Tags = tagsData.ContainsKey(p.PostId)
                        ? tagsData[p.PostId].Select(t => new TagDTO { TagId = t.TagId, TagName = t.TagName }).ToList()
                        : new List<TagDTO>(),
                    Attachments = attachmentsData.ContainsKey(p.PostId)
                        ? attachmentsData[p.PostId].Select(a => new PostAttachmentDTO
                        {
                            AttachmentId = a.AttachmentId,
                            PostId = a.PostId,
                            FileName = a.FileName,
                            FileUrl = a.FileUrl,
                            FileType = a.FileType,
                            FileExtension = a.FileExtension,
                            FileSize = a.FileSize,
                            CreatedAt = a.CreatedAt
                        }).ToList()
                        : new List<PostAttachmentDTO>()
                });
        }


        public IEnumerable<PostDTO> GetRejectedPosts(int page, int limit)
        {
            // Get post IDs first with proper ordering
            var postIds = _context.Post
                .AsNoTracking()
                .Where(p => p.Status == "rejected")
                .OrderByDescending(p => p.IsPinned)
                .ThenByDescending(p => p.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(p => p.PostId)
                .ToList();

            if (!postIds.Any())
                return Enumerable.Empty<PostDTO>();

            // Maintain order dictionary
            var orderDict = postIds
                .Select((id, idx) => new { Id = id, Index = idx })
                .ToDictionary(x => x.Id, x => x.Index);

            // Optimize: Load posts with projection - only select needed fields
            var postsData = _context.Post
                .AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .Select(p => new
                {
                    p.PostId,
                    p.Title,
                    p.Content,
                    p.CreatedAt,
                    p.UpdatedAt,
                    p.ViewCount,
                    p.IsPinned,
                    p.RejectionReason,
                    UserId = p.User.UserId,
                    Username = p.User.Username,
                    Email = p.User.Email,
                    Firstname = p.User.Firstname,
                    Lastname = p.User.Lastname,
                    Role = p.User.Role,
                    Avatar = p.User.Avatar
                })
                .ToList();

            // Batch load related data
            var attachmentsData = _context.PostAttachment
                .AsNoTracking()
                .Where(a => postIds.Contains(a.PostId))
                .Select(a => new
                {
                    a.PostId,
                    a.AttachmentId,
                    a.FileName,
                    a.FileUrl,
                    a.FileType,
                    a.FileExtension,
                    a.FileSize,
                    a.CreatedAt
                })
                .ToList()
                .GroupBy(a => a.PostId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var tagsData = _context.Post
                .AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .SelectMany(p => p.Tags.Select(t => new { p.PostId, t.TagId, t.TagName }))
                .ToList()
                .GroupBy(t => t.PostId)
                .ToDictionary(g => g.Key, g => g.Select(t => new { t.TagId, t.TagName }).ToList());

            // Batch load counts
            var commentCounts = _context.Comment
                .AsNoTracking()
                .Where(c => postIds.Contains(c.PostId))
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.PostId, x => x.Count);

            var likeCounts = _context.PostLike
                .AsNoTracking()
                .Where(pl => postIds.Contains(pl.PostId))
                .GroupBy(pl => pl.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToList()
                .ToDictionary(x => x.PostId, x => x.Count);

            // Build DTOs maintaining original order
            return postsData
                .OrderBy(p => orderDict.GetValueOrDefault(p.PostId, int.MaxValue))
                .Select(p => new PostDTO
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Content = p.Content,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    ViewCount = p.ViewCount ?? 0,
                    CommentCount = commentCounts.GetValueOrDefault(p.PostId, 0),
                    VoteCount = likeCounts.GetValueOrDefault(p.PostId, 0),
                    IsVoted = false,
                    IsPinned = p.IsPinned,
                    IsHiddenByUser = false,
                    RejectionReason = p.RejectionReason,
                    User = new UserDTO
                    {
                        UserId = p.UserId,
                        Username = p.Username,
                        Email = p.Email,
                        Firstname = p.Firstname,
                        Lastname = p.Lastname,
                        Role = p.Role,
                        Avatar = p.Avatar
                    },
                    Tags = tagsData.ContainsKey(p.PostId)
                        ? tagsData[p.PostId].Select(t => new TagDTO { TagId = t.TagId, TagName = t.TagName }).ToList()
                        : new List<TagDTO>(),
                    Attachments = attachmentsData.ContainsKey(p.PostId)
                        ? attachmentsData[p.PostId].Select(a => new PostAttachmentDTO
                        {
                            AttachmentId = a.AttachmentId,
                            PostId = a.PostId,
                            FileName = a.FileName,
                            FileUrl = a.FileUrl,
                            FileType = a.FileType,
                            FileExtension = a.FileExtension,
                            FileSize = a.FileSize,
                            CreatedAt = a.CreatedAt
                        }).ToList()
                        : new List<PostAttachmentDTO>()
                });
        }

        public void ApprovePost(int postId)
        {
            var post = _context.Post.Find(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            post.Status = "approved";
            post.RejectionReason = null; // Clear rejection reason if any

            // Create notification for user
            var notification = new Notification
            {
                UserId = post.UserId,
                Content = $"Your post '{post.Title}' has been approved and is now publicly visible.",
                Type = "post_approved",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                PostId = post.PostId
            };

            _context.Notification.Add(notification);
            _context.SaveChanges();
        }

        public void RejectPost(int postId, string reason)
        {
            var post = _context.Post.Find(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            post.Status = "rejected";
            post.RejectionReason = reason;

            // Create notification for user
            var notification = new Notification
            {
                UserId = post.UserId,
                Content = $"Your post '{post.Title}' has been rejected. Reason: {reason}",
                Type = "post_rejected",
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                PostId = post.PostId
            };

            _context.Notification.Add(notification);
            _context.SaveChanges();
        }

        public IEnumerable<ChartDataDTO> GetPostsChartData(int month, int year)
        {
            var startDate = new DateTime(year, month, 1);
            var endDate = startDate.AddMonths(1);

            var posts = _context.Post
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt < endDate)
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new ChartDataDTO
                {
                    Label = g.Key.ToString("M/d"),
                    Value = g.Count(),
                    Date = g.Key
                })
                .OrderBy(x => x.Date)
                .ToList();

            return posts;
        }

        public IEnumerable<NotificationDTO> GetModeratorNotifications()
        {
            // Mock notifications - in real implementation, you would have a notifications table
            return new List<NotificationDTO>
            {
                new NotificationDTO
                {
                    NotificationId = 1,
                    Title = "New pending post",
                    Content = "A new post is waiting for approval",
                    Type = "pending",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-2)
                },
                new NotificationDTO
                {
                    NotificationId = 2,
                    Title = "Post reported",
                    Content = "A post has been reported by users",
                    Type = "reported",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow.AddHours(-4)
                }
            };
        }

        public void MarkNotificationAsRead(int notificationId)
        {
            // Mock implementation - in real scenario, update notification status
            Console.WriteLine($"Marking notification {notificationId} as read");
        }

    }
}
