using WebAPI.DTOs;

namespace WebAPI.Services
{
    public interface ITagService
    {
        List<TagDTO> GetAllTags();
        TagDTO? GetTagById(int id);
        TagDTO? GetTagByName(string tagName);
        List<TagDTO> SearchTags(string query);
        TagDTO CreateTag(CreateTagDTO dto);
        TagDTO? UpdateTag(int id, UpdateTagDTO dto);
        bool DeleteTag(int id);
    }
}
