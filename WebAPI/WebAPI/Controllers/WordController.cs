using Microsoft.AspNetCore.Mvc;
using WebAPI.DTOs;
using WebAPI.ExternalServices;
using WebAPI.Models;
using WebAPI.Services;
using System.Text.Json;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/words")]
    public class WordsController : ControllerBase
    {
        private readonly IWordService _wordService;
        private readonly IOpenAIService _openAI;

        public WordsController(IWordService wordService, IOpenAIService openAI)
        {
            _wordService = wordService;
            _openAI = openAI;
        }

        // ================================
        // == CRUD METHODS ==
        // ================================

        [HttpGet("{id:int}")]
        public IActionResult GetById(int id)
        {
            var word = _wordService.GetById(id);
            if (word == null) return NotFound();
            return Ok(ToDto(word));
        }

        [HttpGet]
        public IActionResult GetByTerm([FromQuery] string? term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest("Term is required.");

            var word = _wordService.GetByName(term);
            if (word == null) return NotFound();
            return Ok(ToDto(word));
        }

        [HttpGet("search")]
        public IActionResult Search([FromQuery] string keyword)
        {
            var results = _wordService.Search(keyword);
            return Ok(results.Select(ToDto));
        }

        [HttpPost]
        public IActionResult Add([FromBody] WordDto dto)
        {
            var word = new Word
            {
                Term = dto.Term,
                Meaning = dto.Meaning,
                Audio = dto.Audio,
                Example = dto.Example,
                Groups = dto.GroupIds.Select(id => new VocabGroup { GroupId = id }).ToList()
            };
            _wordService.Add(word);
            return CreatedAtAction(nameof(GetById), new { id = word.WordId }, ToDto(word));
        }

        [HttpPut("{id:int}")]
        public IActionResult Update(int id, [FromBody] WordDto dto)
        {
            var word = _wordService.GetById(id);
            if (word == null) return NotFound();

            word.Term = dto.Term;
            word.Meaning = dto.Meaning;
            word.Audio = dto.Audio;
            word.Example = dto.Example;
            word.Groups = dto.GroupIds.Select(gid => new VocabGroup { GroupId = gid }).ToList();

            _wordService.Update(word);
            return NoContent();
        }

        [HttpDelete("{id:int}")]
        public IActionResult Delete(int id)
        {
            var word = _wordService.GetById(id);
            if (word == null) return NotFound();

            _wordService.Delete(id);
            return NoContent();
        }

        [HttpGet("{term}/lookup")]
        public IActionResult Lookup(string term)
        {
            var word = _wordService.LookupOrFetch(term);
            if (word == null) return NotFound();
            return Ok(ToDto(word));
        }



        [HttpPost("lookup-ai")]
        public IActionResult LookupAI([FromBody] AiLookupRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Word))
                return BadRequest("Word is required.");

            try
            {
                var doc = _openAI.LookupWordAI(request.Word);
                var root = doc.RootElement;

                if (!root.TryGetProperty("term", out _))
                    return StatusCode(500, new { message = "Invalid response from AI." });

                // Back-compat mapping nếu model cũ trả ...Meaning
                string detected = root.TryGetProperty("detected_language", out var dl) ? dl.GetString() ?? "" : "";
                string engTrans =
                    root.TryGetProperty("englishTranslation", out var et) ? et.GetString() ?? "" :
                    root.TryGetProperty("englishMeaning", out var em) ? em.GetString() ?? "" : "";
                string vieTrans =
                    root.TryGetProperty("vietnameseTranslation", out var vt) ? vt.GetString() ?? "" :
                    root.TryGetProperty("vietnameseMeaning", out var vm) ? vm.GetString() ?? "" : "";
                string term = root.GetProperty("term").GetString() ?? request.Word;
                string example = root.TryGetProperty("example", out var ex) ? ex.GetString() ?? "" : "";

                var payload = new
                {
                    term,
                    detected_language = detected,
                    englishTranslation = engTrans,
                    vietnameseTranslation = vieTrans,
                    example
                };

                return Ok(payload);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        // Helper: convert entity → DTO
        private static WordDto ToDto(Word word) =>
            new WordDto
            {
                WordId = word.WordId,
                Term = word.Term,
                Meaning = word.Meaning,
                Audio = word.Audio,
                Example = word.Example,
                GroupIds = word.Groups?.Select(g => g.GroupId).ToList() ?? new List<int>()
            };
    }

    public class AiLookupRequest
    {
        public string Word { get; set; } = string.Empty;
        public string? Context { get; set; }
        public string? UserEssay { get; set; }
    }
}
