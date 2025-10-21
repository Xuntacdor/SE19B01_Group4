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

        public async Task<List<TagDTO>> GetAllTagsAsync()
        {
            var tags = await _context.Tag
                .Include(t => t.Posts)
                .OrderBy(t => t.TagName)
                .ToListAsync();

            return tags.Select(t => new TagDTO
            {
                TagId = t.TagId,
                TagName = t.TagName,
                CreatedAt = t.CreatedAt,
                PostCount = t.Posts.Count
            }).ToList();
        }

        public async Task<TagDTO?> GetTagByIdAsync(int id)
        {
            var tag = await _context.Tag
                .Include(t => t.Posts)
                .FirstOrDefaultAsync(t => t.TagId == id);

            if (tag == null) return null;

            return new TagDTO
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                CreatedAt = tag.CreatedAt,
                PostCount = tag.Posts.Count
            };
        }

        public async Task<TagDTO?> GetTagByNameAsync(string tagName)
        {
            var tag = await _context.Tag
                .Include(t => t.Posts)
                .FirstOrDefaultAsync(t => t.TagName.ToLower() == tagName.ToLower());

            if (tag == null) return null;

            return new TagDTO
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                CreatedAt = tag.CreatedAt,
                PostCount = tag.Posts.Count
            };
        }

        public async Task<List<TagDTO>> SearchTagsAsync(string query)
        {
            var tags = await _context.Tag
                .Include(t => t.Posts)
                .Where(t => t.TagName.ToLower().Contains(query.ToLower()))
                .OrderBy(t => t.TagName)
                .ToListAsync();

            return tags.Select(t => new TagDTO
            {
                TagId = t.TagId,
                TagName = t.TagName,
                CreatedAt = t.CreatedAt,
                PostCount = t.Posts.Count
            }).ToList();
        }

        public async Task<TagDTO> CreateTagAsync(CreateTagDTO dto)
        {
            var tag = new Tag
            {
                TagName = dto.TagName.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.Tag.Add(tag);
            await _context.SaveChangesAsync();

            return new TagDTO
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                CreatedAt = tag.CreatedAt,
                PostCount = 0
            };
        }

        public async Task<TagDTO?> UpdateTagAsync(int id, UpdateTagDTO dto)
        {
            var tag = await _context.Tag
                .Include(t => t.Posts)
                .FirstOrDefaultAsync(t => t.TagId == id);

            if (tag == null) return null;

            tag.TagName = dto.TagName.Trim();
            await _context.SaveChangesAsync();

            return new TagDTO
            {
                TagId = tag.TagId,
                TagName = tag.TagName,
                CreatedAt = tag.CreatedAt,
                PostCount = tag.Posts.Count
            };
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            var tag = await _context.Tag
                .Include(t => t.Posts)
                .FirstOrDefaultAsync(t => t.TagId == id);

            if (tag == null) return false;

            // Check if tag is being used by any posts
            if (tag.Posts.Any())
            {
                throw new InvalidOperationException("Cannot delete tag that is being used by posts");
            }

            _context.Tag.Remove(tag);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
