using WebAPI.DTOs;

namespace WebAPI.Services
{
    public interface ITagService
    {
        Task<List<TagDTO>> GetAllTagsAsync();
        Task<TagDTO?> GetTagByIdAsync(int id);
        Task<TagDTO?> GetTagByNameAsync(string tagName);
        Task<List<TagDTO>> SearchTagsAsync(string query);
        Task<TagDTO> CreateTagAsync(CreateTagDTO dto);
        Task<TagDTO?> UpdateTagAsync(int id, UpdateTagDTO dto);
        Task<bool> DeleteTagAsync(int id);
    }
}
