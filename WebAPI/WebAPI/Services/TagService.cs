using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.DTOs;
using WebAPI.Models;

namespace WebAPI.Services
{
    public class TagService : ITagService
    {
        private readonly ApplicationDbContext _context;

        public TagService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<TagDTO> GetAllTags()
        {
            // Optimize: Don't load all Posts, instead count from Post_Tag junction table
            var tags = _context.Tag
                .AsNoTracking()
                .OrderBy(t => t.TagName)
                .Select(t => new
                {
                    t.TagId,
                    t.TagName,
                    t.CreatedAt,
                    PostCount = t.Posts.Count
                })
                .ToList();

            return tags.Select(t => new TagDTO
            {
                TagId = t.TagId,
                TagName = t.TagName,
                CreatedAt = t.CreatedAt,
                PostCount = t.PostCount
            }).ToList();
        }

        public TagDTO? GetTagById(int id)
        {
            // Optimize: Don't load all Posts, instead count from Post_Tag junction table
            var tag = _context.Tag
                .AsNoTracking()
                .Where(t => t.TagId == id)
                .Select(t => new
                {
                    t.TagId,
                    t.TagName,
                    t.CreatedAt,
                    PostCount = t.Posts.Count
                })
                .FirstOrDefault();

            if (tag == null) return null;

            return new TagDTO
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                CreatedAt = tag.CreatedAt,
                PostCount = tag.PostCount
            };
        }

        public TagDTO? GetTagByName(string tagName)
        {
            // Optimize: Don't load all Posts, instead count from Post_Tag junction table
            var tag = _context.Tag
                .AsNoTracking()
                .Where(t => t.TagName.ToLower() == tagName.ToLower())
                .Select(t => new
                {
                    t.TagId,
                    t.TagName,
                    t.CreatedAt,
                    PostCount = t.Posts.Count
                })
                .FirstOrDefault();

            if (tag == null) return null;

            return new TagDTO
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                CreatedAt = tag.CreatedAt,
                PostCount = tag.PostCount
            };
        }

        public List<TagDTO> SearchTags(string query)
        {
            // Optimize: Don't load all Posts, instead count from Post_Tag junction table
            var tags = _context.Tag
                .AsNoTracking()
                .Where(t => t.TagName.ToLower().Contains(query.ToLower()))
                .OrderBy(t => t.TagName)
                .Select(t => new
                {
                    t.TagId,
                    t.TagName,
                    t.CreatedAt,
                    PostCount = t.Posts.Count
                })
                .ToList();

            return tags.Select(t => new TagDTO
            {
                TagId = t.TagId,
                TagName = t.TagName,
                CreatedAt = t.CreatedAt,
                PostCount = t.PostCount
            }).ToList();
        }

        public TagDTO CreateTag(CreateTagDTO dto)
        {
            var tag = new Tag
            {
                TagName = dto.TagName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Tag.Add(tag);
            _context.SaveChanges();

            return new TagDTO
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                CreatedAt = tag.CreatedAt,
                PostCount = 0
            };
        }

        public TagDTO? UpdateTag(int id, UpdateTagDTO dto)
        {
            var tag = _context.Tag
                .FirstOrDefault(t => t.TagId == id);

            if (tag == null) return null;

            tag.TagName = dto.TagName.Trim();
            _context.SaveChanges();

            // Optimize: Load PostCount separately without loading all Posts
            var postCount = _context.Tag
                .AsNoTracking()
                .Where(t => t.TagId == id)
                .Select(t => t.Posts.Count)
                .FirstOrDefault();

            return new TagDTO
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                CreatedAt = tag.CreatedAt,
                PostCount = postCount
            };
        }

        public bool DeleteTag(int id)
        {
            var tag = _context.Tag
                .FirstOrDefault(t => t.TagId == id);

            if (tag == null) return false;

            // Optimize: Check if tag is being used by counting from Post_Tag junction table
            var isUsed = _context.Tag
                .AsNoTracking()
                .Where(t => t.TagId == id)
                .Select(t => t.Posts.Any())
                .FirstOrDefault();

            if (isUsed)
            {
                throw new InvalidOperationException("Cannot delete tag that is being used by posts");
            }

            _context.Tag.Remove(tag);
            _context.SaveChanges();
            return true;
        }
    }
}
