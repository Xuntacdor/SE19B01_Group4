using System.Net.Http;
using System.Text.Json;
using WebAPI.Models;

namespace WebAPI.ExternalServices
{
    public class DictionaryApiClient
    {
        const string BaseUrl = "https://api.dictionaryapi.dev/api/v2/entries/en/";
        private readonly HttpClient _httpClient;

        public DictionaryApiClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        public Word? GetWord(string term)
        {
            var url = BaseUrl + term;
            var response = _httpClient.GetAsync(url).Result;
            if (!response.IsSuccessStatusCode)
                return null;

            var json = response.Content.ReadAsStringAsync().Result;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                return null;

            var entry = root[0];
            string wordText = entry.GetProperty("word").GetString() ?? term;

            string? meaning = null;
            string? example = null;

            // ✅ Lấy nghĩa đầu tiên duy nhất
            if (entry.TryGetProperty("meanings", out var meanings) && meanings.GetArrayLength() > 0)
            {
                var firstMeaning = meanings[0];

                if (firstMeaning.TryGetProperty("definitions", out var definitions) && definitions.GetArrayLength() > 0)
                {
                    var firstDef = definitions[0];

                    if (firstDef.TryGetProperty("definition", out var defProp))
                        meaning = defProp.GetString();

                    if (firstDef.TryGetProperty("example", out var exProp))
                        example = exProp.GetString();
                }
            }

            // ✅ Lấy audio (nếu có)
            string? audio = null;
            if (entry.TryGetProperty("phonetics", out var phonetics) && phonetics.GetArrayLength() > 0)
            {
                foreach (var phonetic in phonetics.EnumerateArray())
                {
                    if (phonetic.TryGetProperty("audio", out var audioProp) && !string.IsNullOrWhiteSpace(audioProp.GetString()))
                    {
                        audio = audioProp.GetString();
                        break;
                    }
                }
            }

            return new Word
            {
                Term = wordText,
                Meaning = meaning,
                Example = example,
                Audio = audio
            };
        }
    }
}
