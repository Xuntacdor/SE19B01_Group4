using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;

namespace WebAPI.Services
{
    public class CommentService : ICommentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserRepository _userRepository;

        public CommentService(ApplicationDbContext context, IUserRepository userRepository)
        {
            _context = context;
            _userRepository = userRepository;
        }

        public IEnumerable<CommentDTO> GetCommentsByPostId(int postId, int? userId = null)
        {
            // Optimize: Load all comments with eager loading and AsNoTracking for read-only
            var allComments = _context.Comment
                .AsNoTracking()
                .Include(c => c.User)
                .Include(c => c.CommentLikes)
                .Where(c => c.PostId == postId)
                .ToList();

            if (!allComments.Any())
                return Enumerable.Empty<CommentDTO>();

            // Optimize: Batch load user-specific data (which comments user has liked) to avoid checking in memory
            HashSet<int>? userLikedCommentIds = null;
            if (userId.HasValue)
            {
                var commentIds = allComments.Select(c => c.CommentId).ToList();
                userLikedCommentIds = _context.CommentLike
                    .AsNoTracking()
                    .Where(cl => commentIds.Contains(cl.CommentId) && cl.UserId == userId.Value)
                    .Select(cl => cl.CommentId)
                    .ToHashSet();
            }

            // Chỉ lấy root comments
            var rootComments = allComments
                .Where(c => c.ParentCommentId == null)
                .OrderBy(c => c.CreatedAt)
                .ToList();

            return rootComments.Select(c => ToDTOWithRepliesOptimized(c, allComments, userId, userLikedCommentIds));
        }

        public CommentDTO? GetCommentById(int id, int? currentUserId = null)
        {
            var comment = _context.Comment
                .Include(c => c.User)
                .Include(c => c.InverseParentComment)
                .Include(c => c.CommentLikes)
                .FirstOrDefault(c => c.CommentId == id);

            if (comment == null) return null;

            return ToDTO(comment, currentUserId);
        }

        public CommentDTO CreateComment(int postId, CreateCommentDTO dto, int userId)
        {
            var post = _context.Post.Find(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            var user = _userRepository.GetById(userId);
            if (user == null) throw new KeyNotFoundException("User not found");
            
            // Check if user is restricted
            if (user.IsRestricted)
                throw new UnauthorizedAccessException("Your account has been restricted from commenting on the forum. Please contact support.");

            var comment = new Comment
            {
                PostId = postId,
                UserId = userId,
                Content = dto.Content,
                ParentCommentId = dto.ParentCommentId,
                LikeNumber = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comment.Add(comment);
            _context.SaveChanges();

            return GetCommentById(comment.CommentId) ?? throw new InvalidOperationException("Failed to create comment");
        }

        public CommentDTO CreateReply(int parentCommentId, CreateCommentDTO dto, int userId)
        {
            var parentComment = _context.Comment.Find(parentCommentId);
            if (parentComment == null) throw new KeyNotFoundException("Parent comment not found");

            var user = _userRepository.GetById(userId);
            if (user == null) throw new KeyNotFoundException("User not found");

            var reply = new Comment
            {
                PostId = parentComment.PostId,
                UserId = userId,
                Content = dto.Content,
                ParentCommentId = parentCommentId,
                LikeNumber = 0,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comment.Add(reply);
            _context.SaveChanges();

            return GetCommentById(reply.CommentId) ?? throw new InvalidOperationException("Failed to create reply");
        }

        public void UpdateComment(int id, UpdateCommentDTO dto, int userId)
        {
            var comment = _context.Comment.Find(id);
            if (comment == null) throw new KeyNotFoundException("Comment not found");

            var user = _userRepository.GetById(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");

            if (comment.UserId != userId && user.Role != "admin")
                throw new UnauthorizedAccessException("You don't have permission to update this comment");

            comment.Content = dto.Content;
            _context.SaveChanges();
        }

        public void DeleteComment(int id, int userId)
        {
            var comment = _context.Comment
                .Include(c => c.Post)
                .FirstOrDefault(c => c.CommentId == id);
            if (comment == null) throw new KeyNotFoundException("Comment not found");

            var user = _userRepository.GetById(userId);
            if (user == null) throw new UnauthorizedAccessException("User not found");

            // Cho phép xóa nếu:
            // 1. User là chủ comment
            // 2. User là admin
            // 3. User là chủ bài viết (post owner)
            bool canDelete = comment.UserId == userId || 
                            user.Role == "admin" || 
                            comment.Post.UserId == userId;

            if (!canDelete)
                throw new UnauthorizedAccessException("You don't have permission to delete this comment");

            try
            {
                // Xóa tất cả nested comments (replies) trước
                var nestedComments = _context.Comment.Where(c => c.ParentCommentId == id).ToList();
                if (nestedComments.Any())
                {
                    // Xóa tất cả CommentLikes của nested comments
                    var nestedCommentLikes = _context.CommentLike
                        .Where(cl => nestedComments.Select(c => c.CommentId).Contains(cl.CommentId))
                        .ToList();
                    _context.CommentLike.RemoveRange(nestedCommentLikes);
                    
                    // Xóa nested comments
                    _context.Comment.RemoveRange(nestedComments);
                }
                
                // Xóa tất cả CommentLikes của comment chính
                var commentLikes = _context.CommentLike.Where(cl => cl.CommentId == id).ToList();
                _context.CommentLike.RemoveRange(commentLikes);
                
                // Xóa comment chính
                _context.Comment.Remove(comment);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting comment: {ex.Message}", ex);
            }
        }

        public void LikeComment(int id, int userId)
        {
            var comment = _context.Comment.Find(id);
            if (comment == null) throw new KeyNotFoundException("Comment not found");

            // Check if user already liked this comment
            var existingLike = _context.CommentLike
                .FirstOrDefault(cl => cl.CommentId == id && cl.UserId == userId);

            if (existingLike != null)
                throw new InvalidOperationException("You have already liked this comment");

            // Add new like record
            var commentLike = new CommentLike
            {
                CommentId = id,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.CommentLike.Add(commentLike);
            _context.SaveChanges();
        }

        public void UnlikeComment(int id, int userId)
        {
            var comment = _context.Comment.Find(id);
            if (comment == null) throw new KeyNotFoundException("Comment not found");

            // Check if user has liked this comment
            var existingLike = _context.CommentLike
                .FirstOrDefault(cl => cl.CommentId == id && cl.UserId == userId);

            if (existingLike == null)
                throw new InvalidOperationException("You haven't liked this comment");

            // Remove like record
            _context.CommentLike.Remove(existingLike);
            _context.SaveChanges();
        }

        public void ReportComment(int id, string reason, int userId)
        {
            var comment = _context.Comment.Find(id);
            if (comment == null) throw new KeyNotFoundException("Comment not found");

            var report = new Report
            {
                UserId = userId,
                CommentId = id,
                CommentAuthorUserId = comment.UserId, // Store original comment author for statistics
                Content = reason,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Report.Add(report);
            _context.SaveChanges();
        }

        public IEnumerable<ReportedCommentDTO> GetReportedComments(int page, int limit)
        {
            var reportedComments = _context.Report
                .Include(r => r.Comment)
                .ThenInclude(c => c.User)
                .Include(r => r.Comment)
                .ThenInclude(c => c.Post)
                .Where(r => r.Status == "Pending" && r.CommentId.HasValue)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .ToList();

            return reportedComments.Select(r => new ReportedCommentDTO
            {
                ReportId = r.ReportId,
                CommentId = r.CommentId ?? 0,
                Content = r.Comment?.Content ?? "",
                Author = r.Comment?.User?.Username ?? "",
                CreatedAt = r.Comment?.CreatedAt ?? DateTime.UtcNow,
                PostTitle = r.Comment?.Post?.Title ?? "",
                ReportReason = r.Content,
                ReportCount = 1, // In a real implementation, you might count multiple reports for the same comment
                Status = "Reported"
            });
        }

        public void ApproveReport(int reportId)
        {
            var report = _context.Report
                .Include(r => r.Comment)
                .ThenInclude(c => c.CommentLikes)
                .FirstOrDefault(r => r.ReportId == reportId);
            
            if (report == null) throw new KeyNotFoundException("Report not found");

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                if (report.Comment != null)
                {
                    // Ensure CommentAuthorUserId is populated for this report and related reports
                    var allReportsForComment = _context.Report
                        .Include(r => r.Comment)
                        .Where(r => r.CommentId.HasValue && r.CommentId == report.Comment.CommentId)
                        .ToList();
                    
                    bool isFirstReport = true;
                    foreach (var r in allReportsForComment)
                    {
                        // Only mark the first report (the one moderator clicked) as "Approved"
                        // Others are marked as "Resolved" so they don't count in statistics
                        if (isFirstReport && r.ReportId == reportId)
                        {
                            r.Status = "Approved";
                            isFirstReport = false;
                        }
                        else
                        {
                            r.Status = "Resolved"; // Don't count in statistics
                        }
                        
                        // Ensure CommentAuthorUserId is set
                        if (!r.CommentAuthorUserId.HasValue && r.Comment != null)
                        {
                            r.CommentAuthorUserId = r.Comment.UserId;
                        }
                    }
                    _context.SaveChanges(); // Save the approved status first
                    
                    // Create notification for comment author
                    var commentAuthorUserId = report.CommentAuthorUserId ?? report.Comment.UserId;
                    Console.WriteLine($"[DEBUG] Creating notification for user {commentAuthorUserId}");
                    var notification = new Notification
                    {
                        UserId = commentAuthorUserId,
                        Content = $"Your comment has been deleted because it violated community guidelines. If you has beed reported more than 3 times, your account will be not allowed to comment or post on forum. Please contact the moderator if you think this is a mistake.",
                        Type = "comment_deleted",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notification.Add(notification);
                    _context.SaveChanges(); // Save notification before deleting comment
                    Console.WriteLine($"[DEBUG] Notification created successfully with ID: {notification.NotificationId}");
                    
                    // Then delete the reported comment and all related data EXCEPT reports
                    DeleteCommentForModeratorKeepReports(report.Comment.CommentId);
                }
                else
                {
                    // If comment no longer exists, just mark the report as approved
                    report.Status = "Approved";
                }
                
                _context.SaveChanges();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception in ApproveReport: {ex.Message}");
                Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
                transaction.Rollback();
                throw;
            }
        }

        private void DeleteCommentForModerator(int commentId)
        {
            // Get all comments in this tree (including nested ones)
            var allCommentIds = GetAllCommentIdsInTree(commentId);
            
            // First, break all parent-child relationships by setting parent_comment_id to NULL
            var commentsToUpdate = _context.Comment
                .Where(c => allCommentIds.Contains(c.CommentId) && c.ParentCommentId.HasValue)
                .ToList();
            
            foreach (var comment in commentsToUpdate)
            {
                comment.ParentCommentId = null;
            }
            _context.SaveChanges(); // Save the relationship changes first
            
            // Now delete all related data
            var commentLikes = _context.CommentLike
                .Where(cl => allCommentIds.Contains(cl.CommentId))
                .ToList();
            _context.CommentLike.RemoveRange(commentLikes);
            
            var reports = _context.Report
                .Where(r => r.CommentId.HasValue && allCommentIds.Contains(r.CommentId.Value))
                .ToList();
            _context.Report.RemoveRange(reports);
            
            // Finally, delete all comments
            var commentsToDelete = _context.Comment
                .Where(c => allCommentIds.Contains(c.CommentId))
                .ToList();
            _context.Comment.RemoveRange(commentsToDelete);
        }
        
        private void DeleteCommentForModeratorKeepReports(int commentId)
        {
            // Get all comments in this tree (including nested ones)
            var allCommentIds = GetAllCommentIdsInTree(commentId);
            
            // First, break all parent-child relationships by setting parent_comment_id to NULL
            var commentsToUpdate = _context.Comment
                .Where(c => allCommentIds.Contains(c.CommentId) && c.ParentCommentId.HasValue)
                .ToList();
            
            foreach (var comment in commentsToUpdate)
            {
                comment.ParentCommentId = null;
            }
            _context.SaveChanges(); // Save the relationship changes first
            
            // Delete CommentLikes
            var commentLikes = _context.CommentLike
                .Where(cl => allCommentIds.Contains(cl.CommentId))
                .ToList();
            _context.CommentLike.RemoveRange(commentLikes);
            
            // Update Reports to break comment reference but keep the CommentAuthorUserId for statistics
            var reports = _context.Report
                .Include(r => r.Comment)
                .Where(r => r.CommentId.HasValue && allCommentIds.Contains(r.CommentId.Value))
                .ToList();
            foreach (var report in reports)
            {
                // If CommentAuthorUserId is not set, get it from the comment before breaking the reference
                if (!report.CommentAuthorUserId.HasValue && report.Comment != null)
                {
                    report.CommentAuthorUserId = report.Comment.UserId;
                }
                // Break the reference to the comment by setting CommentId to NULL
                // This keeps the report for statistics but removes the foreign key constraint
                report.CommentId = null;
            }
            
            // Finally, delete all comments
            var commentsToDelete = _context.Comment
                .Where(c => allCommentIds.Contains(c.CommentId))
                .ToList();
            _context.Comment.RemoveRange(commentsToDelete);
        }
        
        private List<int> GetAllCommentIdsInTree(int commentId)
        {
            var allIds = new List<int> { commentId };
            
            // Get all direct children
            var childComments = _context.Comment
                .Where(c => c.ParentCommentId == commentId)
                .Select(c => c.CommentId)
                .ToList();
            
            // Recursively get all nested children
            foreach (var childId in childComments)
            {
                allIds.AddRange(GetAllCommentIdsInTree(childId));
            }
            
            return allIds;
        }

        public void DismissReport(int reportId)
        {
            var report = _context.Report.Find(reportId);
            if (report == null) throw new KeyNotFoundException("Report not found");

            // Mark report as dismissed
            report.Status = "Dismissed";
            _context.SaveChanges();
        }

        private CommentDTO ToDTO(Comment comment, int? currentUserId = null)
        {
            // Check if user has voted for this comment using the loaded CommentLikes
            bool isVoted = false;
            if (currentUserId.HasValue)
            {
                isVoted = comment.CommentLikes.Any(cl => cl.UserId == currentUserId.Value);
            }

            return new CommentDTO
            {
                CommentId = comment.CommentId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                LikeNumber = comment.CommentLikes.Count,
                VoteCount = comment.CommentLikes.Count,
                IsVoted = isVoted,
                ParentCommentId = comment.ParentCommentId,
                User = new UserDTO
                {
                    UserId = comment.User.UserId,
                    Username = comment.User.Username,
                    Email = comment.User.Email,
                    Firstname = comment.User.Firstname,
                    Lastname = comment.User.Lastname,
                    Role = comment.User.Role,
                    Avatar = comment.User.Avatar
                },
                Replies = new List<CommentDTO>() // Don't load replies in ToDTO, use ToDTOWithReplies instead
            };
        }

        private CommentDTO ToDTOWithReplies(Comment comment, List<Comment> allComments, int? currentUserId)
        {
            var replies = allComments
                .Where(c => c.ParentCommentId == comment.CommentId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => ToDTOWithReplies(c, allComments, currentUserId))
                .ToList();

            // Check if user has voted for this comment using the loaded CommentLikes
            bool isVoted = false;
            if (currentUserId.HasValue)
            {
                isVoted = comment.CommentLikes.Any(cl => cl.UserId == currentUserId.Value);
            }

            return new CommentDTO
            {
                CommentId = comment.CommentId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                LikeNumber = comment.CommentLikes.Count,
                VoteCount = comment.CommentLikes.Count,
                IsVoted = isVoted,
                ParentCommentId = comment.ParentCommentId,
                User = new UserDTO
                {
                    UserId = comment.User.UserId,
                    Username = comment.User.Username,
                    Email = comment.User.Email,
                    Firstname = comment.User.Firstname,
                    Lastname = comment.User.Lastname,
                    Role = comment.User.Role,
                    Avatar = comment.User.Avatar
                },
                Replies = replies
            };
        }

        private CommentDTO ToDTOWithRepliesOptimized(Comment comment, List<Comment> allComments, int? currentUserId, HashSet<int>? userLikedCommentIds)
        {
            var replies = allComments
                .Where(c => c.ParentCommentId == comment.CommentId)
                .OrderBy(c => c.CreatedAt)
                .Select(c => ToDTOWithRepliesOptimized(c, allComments, currentUserId, userLikedCommentIds))
                .ToList();

            // Optimize: Use pre-loaded HashSet instead of checking CommentLikes in memory
            bool isVoted = userLikedCommentIds != null && userLikedCommentIds.Contains(comment.CommentId);

            return new CommentDTO
            {
                CommentId = comment.CommentId,
                Content = comment.Content,
                CreatedAt = comment.CreatedAt,
                LikeNumber = comment.CommentLikes.Count,
                VoteCount = comment.CommentLikes.Count,
                IsVoted = isVoted,
                ParentCommentId = comment.ParentCommentId,
                User = new UserDTO
                {
                    UserId = comment.User.UserId,
                    Username = comment.User.Username,
                    Email = comment.User.Email,
                    Firstname = comment.User.Firstname,
                    Lastname = comment.User.Lastname,
                    Role = comment.User.Role,
                    Avatar = comment.User.Avatar
                },
                Replies = replies
            };
        }

        private bool IsCommentVotedByUser(int commentId, int userId)
        {
            return _context.CommentLike
                .Any(cl => cl.CommentId == commentId && cl.UserId == userId);
        }
    }
}
