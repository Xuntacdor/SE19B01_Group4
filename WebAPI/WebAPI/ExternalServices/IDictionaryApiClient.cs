using WebAPI.Models;

namespace WebAPI.ExternalServices
{
    public interface IDictionaryApiClient
    {
        Word? GetWord(string term);
    }
}
