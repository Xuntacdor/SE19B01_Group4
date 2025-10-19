using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using WebAPI.Models;

namespace WebAPI.ExternalServices
{
    public class DictionaryApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public DictionaryApiClient(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;

            _baseUrl = config["ExternalAPIs:ApiNinjas:BaseUrl"]
                       ?? "https://api.api-ninjas.com/v1/dictionary?word=";

            var apiKey = config["ExternalAPIs:ApiNinjas:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
                throw new ArgumentException("API Ninjas key not configured.");

            _httpClient.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        public Word? GetWord(string term)
        {
            var url = _baseUrl + term;

            var response = _httpClient.GetAsync(url).Result;
            if (!response.IsSuccessStatusCode)
                return null;

            var json = response.Content.ReadAsStringAsync().Result;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("definition", out var def))
                return null;

            string? definition = def.GetString();

            if (!string.IsNullOrEmpty(definition))
            {
                // ✅ Lấy nghĩa đầu tiên trước “2.”, “3.” hoặc xuống dòng
                var match = Regex.Match(definition, @"^(.*?)(?:\s+\d+\.)");
                if (match.Success)
                    definition = match.Groups[1].Value.Trim();
                else
                    definition = definition.Split('\n')[0].Trim();

                // ✅ Loại bỏ phần [Obs.], [Colloq.], v.v.
                definition = Regex.Replace(definition, @"\[[^\]]+\]", "").Trim();
            }

            return new Word
            {
                Term = term,
                Meaning = definition,
                Example = null,
                Audio = null
            };
        }
    }
}
