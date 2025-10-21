using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebAPI.DTOs;
using WebAPI.Services;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagController(ITagService tagService)
        {
            _tagService = tagService;
        }

        [HttpGet]
        public async Task<ActionResult<List<TagDTO>>> GetAllTags()
        {
            try
            {
                var tags = await _tagService.GetAllTagsAsync();
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tags", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TagDTO>> GetTagById(int id)
        {
            try
            {
                var tag = await _tagService.GetTagByIdAsync(id);
                if (tag == null)
                    return NotFound(new { message = "Tag not found" });

                return Ok(tag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tag", error = ex.Message });
            }
        }

        [HttpPost]
        [Authorize(Roles = "admin,moderator")]
        public async Task<ActionResult<TagDTO>> CreateTag([FromBody] CreateTagDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.TagName))
                    return BadRequest(new { message = "Tag name is required" });

                var existingTag = await _tagService.GetTagByNameAsync(dto.TagName);
                if (existingTag != null)
                    return Conflict(new { message = "Tag already exists" });

                var tag = await _tagService.CreateTagAsync(dto);
                return CreatedAtAction(nameof(GetTagById), new { id = tag.TagId }, tag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating tag", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin,moderator")]
        public async Task<ActionResult<TagDTO>> UpdateTag(int id, [FromBody] UpdateTagDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.TagName))
                    return BadRequest(new { message = "Tag name is required" });

                var existingTag = await _tagService.GetTagByNameAsync(dto.TagName);
                if (existingTag != null && existingTag.TagId != id)
                    return Conflict(new { message = "Tag name already exists" });

                var tag = await _tagService.UpdateTagAsync(id, dto);
                if (tag == null)
                    return NotFound(new { message = "Tag not found" });

                return Ok(tag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating tag", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "admin,moderator")]
        public async Task<ActionResult> DeleteTag(int id)
        {
            try
            {
                var result = await _tagService.DeleteTagAsync(id);
                if (!result)
                    return NotFound(new { message = "Tag not found" });

                return Ok(new { message = "Tag deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting tag", error = ex.Message });
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<TagDTO>>> SearchTags([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return Ok(new List<TagDTO>());

                var tags = await _tagService.SearchTagsAsync(query);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching tags", error = ex.Message });
            }
        }
    }
}
