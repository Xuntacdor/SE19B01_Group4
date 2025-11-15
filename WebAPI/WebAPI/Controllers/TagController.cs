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
        public ActionResult<List<TagDTO>> GetAllTags()
        {
            try
            {
                var tags = _tagService.GetAllTags();
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving tags", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public ActionResult<TagDTO> GetTagById(int id)
        {
            try
            {
                var tag = _tagService.GetTagById(id);
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
        public ActionResult<TagDTO> CreateTag([FromBody] CreateTagDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.TagName))
                    return BadRequest(new { message = "Tag name is required" });

                var existingTag = _tagService.GetTagByName(dto.TagName);
                if (existingTag != null)
                    return Conflict(new { message = "Tag already exists" });

                var tag = _tagService.CreateTag(dto);
                return CreatedAtAction(nameof(GetTagById), new { id = tag.TagId }, tag);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating tag", error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "admin,moderator")]
        public ActionResult<TagDTO> UpdateTag(int id, [FromBody] UpdateTagDTO dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.TagName))
                    return BadRequest(new { message = "Tag name is required" });

                var existingTag = _tagService.GetTagByName(dto.TagName);
                if (existingTag != null && existingTag.TagId != id)
                    return Conflict(new { message = "Tag name already exists" });

                var tag = _tagService.UpdateTag(id, dto);
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
        public ActionResult DeleteTag(int id)
        {
            try
            {
                var result = _tagService.DeleteTag(id);
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
        public ActionResult<List<TagDTO>> SearchTags([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                    return Ok(new List<TagDTO>());

                var tags = _tagService.SearchTags(query);
                return Ok(tags);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error searching tags", error = ex.Message });
            }
        }
    }
}
