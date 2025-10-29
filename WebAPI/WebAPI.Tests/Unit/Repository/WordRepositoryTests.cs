using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI.Data;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.ExternalServices;
using Xunit;

namespace WebAPI.Tests.Units.Repository
{
    public class WordRepositoryTests
    {
        private ApplicationDbContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void GetById_WhenExists_ReturnsWord()
        {
            using var context = CreateInMemoryContext();
            var word = new Word { WordId = 1, Term = "sample", Meaning = "meaning" };
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.GetById(1);

            Assert.NotNull(result);
            Assert.Equal("sample", result.Term);
        }

        [Fact]
        public void GetByName_WhenExists_ReturnsWord()
        {
            using var context = CreateInMemoryContext();
            var word = new Word { WordId = 1, Term = "test" };
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.GetByName("test");

            Assert.NotNull(result);
        }

        [Fact]
        public void Add_AddsWord()
        {
            using var context = CreateInMemoryContext();
            var word = new Word { Term = "new" };
            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            repo.Add(word);
            repo.SaveChanges();

            Assert.True(context.Word.Any(w => w.Term == "new"));
        }

        [Fact]
        public void Delete_DeletesWord()
        {
            using var context = CreateInMemoryContext();
            var word = new Word { WordId = 1, Term = "to delete", Meaning = "meaning" };
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            repo.Delete(word);
            repo.SaveChanges();

            Assert.False(context.Word.Any(w => w.Term == "to delete"));
        }

        [Fact]
        public void Update_UpdatesWord()
        {
            using var context = CreateInMemoryContext();
            var word = new Word { WordId = 1, Term = "old term", Meaning = "old meaning" };
            context.Word.Add(word);
            context.SaveChanges();

            word.Meaning = "new meaning";
            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            repo.Update(word);
            repo.SaveChanges();

            var updated = context.Word.FirstOrDefault(w => w.WordId == 1);
            Assert.Equal("new meaning", updated.Meaning);
        }

        [Fact]
        public void GetByName_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.GetByName("nonexistent");

            Assert.Null(result);
        }

        [Fact]
        public void GetById_WhenNotExists_ReturnsNull()
        {
            using var context = CreateInMemoryContext();
            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.GetById(999);

            Assert.Null(result);
        }

        [Fact]
        public void GetByName_CaseInsensitive_ReturnsWord()
        {
            using var context = CreateInMemoryContext();
            var word = new Word { WordId = 1, Term = "Test" };
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.GetByName("TEST");

            Assert.NotNull(result);
            Assert.Equal("Test", result.Term);
        }

        [Fact]
        public void AddWordToGroup_AddsWordToGroup()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Test Group", UserId = 1, CreatedAt = DateTime.Now };
            var word = new Word { WordId = 1, Term = "test word" };
            context.VocabGroup.Add(group);
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            repo.AddWordToGroup(1, 1);

            var updatedGroup = context.VocabGroup.FirstOrDefault(g => g.GroupId == 1);
            Assert.Contains(updatedGroup.Words, w => w.WordId == 1);
        }

        [Fact]
        public void AddWordToGroup_GroupNotExists_DoesNothing()
        {
            using var context = CreateInMemoryContext();
            var word = new Word { WordId = 1, Term = "test word" };
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            repo.AddWordToGroup(999, 1);

            var updatedWord = context.Word.FirstOrDefault(w => w.WordId == 1);
            Assert.NotNull(updatedWord);
        }

        [Fact]
        public void RemoveWordFromGroup_RemovesWordFromGroup()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Test Group", UserId = 1, CreatedAt = DateTime.Now };
            var word = new Word { WordId = 1, Term = "test word" };
            group.Words.Add(word);
            context.VocabGroup.Add(group);
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            repo.RemoveWordFromGroup(1, 1);

            var updatedGroup = context.VocabGroup.FirstOrDefault(g => g.GroupId == 1);
            Assert.DoesNotContain(updatedGroup.Words, w => w.WordId == 1);
        }

        [Fact]
        public void GetWordsByGroup_ReturnsWordsInGroup()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Test Group", UserId = 1, CreatedAt = DateTime.Now };
            var word1 = new Word { WordId = 1, Term = "word1" };
            var word2 = new Word { WordId = 2, Term = "word2" };
            group.Words.Add(word1);
            group.Words.Add(word2);
            context.VocabGroup.Add(group);
            context.Word.Add(word1);
            context.Word.Add(word2);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.GetWordsByGroup(1);

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetWordsByGroup_GroupNotExists_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.GetWordsByGroup(999);

            Assert.Empty(result);
        }

        [Fact]
        public void IsWordInGroup_WhenInGroup_ReturnsTrue()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Test Group", UserId = 1, CreatedAt = DateTime.Now };
            var word = new Word { WordId = 1, Term = "test word" };
            group.Words.Add(word);
            context.VocabGroup.Add(group);
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.IsWordInGroup(1, 1);

            Assert.True(result);
        }

        [Fact]
        public void IsWordInGroup_WhenNotInGroup_ReturnsFalse()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Test Group", UserId = 1, CreatedAt = DateTime.Now };
            var word = new Word { WordId = 1, Term = "test word" };
            context.VocabGroup.Add(group);
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.IsWordInGroup(1, 1);

            Assert.False(result);
        }

        [Fact]
        public void SearchWords_ReturnsMatchingWords()
        {
            using var context = CreateInMemoryContext();
            context.Word.Add(new Word { WordId = 1, Term = "apple", Meaning = "fruit" });
            context.Word.Add(new Word { WordId = 2, Term = "orange", Meaning = "color and fruit" });
            context.Word.Add(new Word { WordId = 3, Term = "car", Meaning = "vehicle" });
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.SearchWords("fruit");

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void SearchWords_CaseInsensitive_ReturnsMatches()
        {
            using var context = CreateInMemoryContext();
            context.Word.Add(new Word { WordId = 1, Term = "Apple" });
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.SearchWords("apple");

            Assert.Single(result);
        }

        [Fact]
        public void SearchWords_NoMatches_ReturnsEmpty()
        {
            using var context = CreateInMemoryContext();
            context.Word.Add(new Word { WordId = 1, Term = "apple" });
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.SearchWords("nonexistent");

            Assert.Empty(result);
        }

        [Fact]
        public void SearchWords_WordWithNullMeaning_SkipsMeaningSearch()
        {
            using var context = CreateInMemoryContext();
            context.Word.Add(new Word { WordId = 1, Term = "apple", Meaning = null });
            context.Word.Add(new Word { WordId = 2, Term = "orange", Meaning = "fruit" });
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.SearchWords("fruit");

            Assert.Single(result);
            Assert.Equal("orange", result.First().Term);
        }

        [Fact]
        public void LookupOrFetch_WhenExists_ReturnsExistingWord()
        {
            using var context = CreateInMemoryContext();
            var word = new Word { WordId = 1, Term = "apple", Meaning = "fruit" };
            context.Word.Add(word);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.LookupOrFetch("apple");

            Assert.NotNull(result);
            Assert.Equal("apple", result.Term);
            Assert.Equal("fruit", result.Meaning);
        }

        [Fact]
        public void LookupOrFetch_WhenNotExists_FetchesFromAPI()
        {
            using var context = CreateInMemoryContext();
            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            var result = repo.LookupOrFetch("nonexistent");

            Assert.NotNull(result);
            Assert.Equal("nonexistent", result.Term);
            Assert.Contains("Not existent", result.Meaning); // Approximate
        }

        [Fact]
        public void AddWordToGroup_WordNotExists_DoesNothing()
        {
            using var context = CreateInMemoryContext();
            var group = new VocabGroup { GroupId = 1, Groupname = "Test Group", UserId = 1, CreatedAt = DateTime.Now };
            context.VocabGroup.Add(group);
            context.SaveChanges();

            var api = new DictionaryApiClient(new HttpClient());
            var repo = new WordRepository(context, api);

            repo.AddWordToGroup(1, 999);

            var updatedGroup = context.VocabGroup.FirstOrDefault(g => g.GroupId == 1);
            Assert.Empty(updatedGroup.Words);
        }
    }
}
