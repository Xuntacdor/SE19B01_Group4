using FluentAssertions;
using Moq;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;

namespace WebAPI.Tests
{
    public class ListeningServiceTests
    {
        private readonly Mock<IListeningRepository> _listeningRepoMock;
        private readonly ListeningService _listeningService;
        private static readonly string[] value = new string [] {"B"};

        public ListeningServiceTests()
        {
            _listeningRepoMock = new Mock<IListeningRepository>();
            _listeningService = new ListeningService(_listeningRepoMock.Object);
        }

        private Listening CreateSampleListening(int id = 1, int examId = 1, string correctAnswer = "answer")
        {
            return new Listening
            {
                ListeningId = id,
                ExamId = examId,
                ListeningContent = "Sample content",
                ListeningQuestion = "Sample question",
                ListeningType = "Markdown",
                DisplayOrder = 1,
                CorrectAnswer = correctAnswer,
                QuestionHtml = "<p>Sample</p>",
                CreatedAt = DateTime.UtcNow
            };
        }

        private void SetupMockRepository(List<Listening> initialListenings)
        {
            var nextId = initialListenings.Any()
                ? initialListenings.Max(r => r.ListeningId) + 1
                : 1;

            _listeningRepoMock.Setup(repo => repo.GetById(It.IsAny<int>()))
                .Returns<int>(id => initialListenings.FirstOrDefault(r => r.ListeningId == id));

            _listeningRepoMock.Setup(repo => repo.GetAll()).Returns(initialListenings);

            _listeningRepoMock.Setup(repo => repo.GetByExamId(It.IsAny<int>()))
                .Returns<int>(examId =>
                    initialListenings.Where(r => r.ExamId == examId).ToList());

            _listeningRepoMock.Setup(repo => repo.Add(It.IsAny<Listening>()))
                .Callback<Listening>(r =>
                {
                    if (r.ListeningId == 0)
                    {
                        r.ListeningId = nextId++;
                    }
                    initialListenings.Add(r);
                });

            _listeningRepoMock.Setup(repo => repo.Update(It.IsAny<Listening>()))
                .Callback<Listening>(r =>
                {
                    var index = initialListenings.FindIndex(x => x.ListeningId == r.ListeningId);
                    if (index >= 0)
                    {
                        initialListenings[index] = r;
                    }
                });

            _listeningRepoMock.Setup(repo => repo.Delete(It.IsAny<Listening>()))
                .Callback<Listening>(r =>
                {
                    initialListenings.RemoveAll(x => x.ListeningId == r.ListeningId);
                });

            _listeningRepoMock.Setup(repo => repo.SaveChanges());
        }

        [Fact]
        public void GetById_WhenListeningExists_ReturnsDto()
        {
            var listenings = new List<Listening> { CreateSampleListening(1), CreateSampleListening(2) };
            SetupMockRepository(listenings);

            var result = _listeningService.GetById(1);

            result.Should().NotBeNull();
            result!.ListeningId.Should().Be(1);
            result.ListeningContent.Should().Be("Sample content");
        }

        [Fact]
        public void GetById_WhenListeningNotFound_ReturnsNull()
        {
            var listenings = new List<Listening>();
            SetupMockRepository(listenings);

            var result = _listeningService.GetById(5);

            result.Should().BeNull();
        }

        [Fact]
        public void GetListeningsByExam_ReturnsListeningsForExam()
        {
            var listenings = new List<Listening>
            {
                CreateSampleListening(1, examId: 1),
                CreateSampleListening(2, examId: 2),
                CreateSampleListening(3, examId: 1)
            };
            SetupMockRepository(listenings);

            var result = _listeningService.GetListeningsByExam(1);

            result.Should().HaveCount(2).And.OnlyContain(r => r.ExamId == 1);
        }

        [Fact]
        public void GetAll_ReturnsAllListeningsAsDto()
        {
            var listenings = new List<Listening> { CreateSampleListening(1), CreateSampleListening(2) };
            SetupMockRepository(listenings);

            var result = _listeningService.GetAll();

            result.Should().HaveCount(2);
            result.Select(r => r.ListeningId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public void Add_ValidDto_AddsListeningAndReturnsDto()
        {
            var listenings = new List<Listening>();
            SetupMockRepository(listenings);
            var dto = new CreateListeningDto
            {
                ExamId = 1,
                ListeningContent = "Content",
                ListeningQuestion = "Question",
                ListeningType = null,
                DisplayOrder = 1,
                CorrectAnswer = "answer",
                QuestionHtml = "<p>Sample</p>"
            };

            var result = _listeningService.Add(dto);

            result.Should().NotBeNull();
            result!.ListeningId.Should().BeGreaterThan(0);
            result.ListeningType.Should().Be("Markdown");
            listenings.Should().ContainSingle();
            listenings[0].ListeningContent.Should().Be("Content");
        }

        [Fact]
        public void Update_WhenListeningExists_UpdatesPropertiesAndReturnsTrue()
        {
            var listening = CreateSampleListening(1);
            var listenings = new List<Listening> { listening };
            SetupMockRepository(listenings);
            var dto = new UpdateListeningDto
            {
                ListeningContent = "Updated content",
                ListeningQuestion = null,
                ListeningType = "HTML",
                DisplayOrder = 2,
                CorrectAnswer = "new answer",
                QuestionHtml = null
            };

            var result = _listeningService.Update(1, dto);

            result.Should().BeTrue();
            listening.ListeningContent.Should().Be("Updated content");
            listening.ListeningType.Should().Be("HTML");
            listening.DisplayOrder.Should().Be(2);
            listening.CorrectAnswer.Should().Be("new answer");
        }

        [Fact]
        public void Update_WhenListeningNotFound_ReturnsFalse()
        {
            var listenings = new List<Listening> { CreateSampleListening(1) };
            SetupMockRepository(listenings);
            var dto = new UpdateListeningDto
            {
                ListeningContent = "Updated"
            };

            var result = _listeningService.Update(99, dto);

            result.Should().BeFalse();
            listenings.Should().HaveCount(1);
        }

        [Fact]
        public void Delete_WhenListeningExists_RemovesListeningAndReturnsTrue()
        {
            var listening = CreateSampleListening(1);
            var listenings = new List<Listening> { listening };
            SetupMockRepository(listenings);

            var result = _listeningService.Delete(1);

            result.Should().BeTrue();
            listenings.Should().BeEmpty();
        }

        [Fact]
        public void Delete_WhenListeningNotFound_ReturnsFalse()
        {
            var listenings = new List<Listening> { CreateSampleListening(1) };
            SetupMockRepository(listenings);

            var result = _listeningService.Delete(2);

            result.Should().BeFalse();
            listenings.Should().HaveCount(1);
        }

        [Fact]
        public void EvaluateListening_NoListenings_ReturnsZero()
        {
            var listenings = new List<Listening>();
            SetupMockRepository(listenings);

            var result = _listeningService.EvaluateListening(1, new List<UserAnswerGroup>());

            result.Should().Be(0m);
        }

        [Fact]
        public void EvaluateListening_CalculatesScoreForJsonAnswers()
        {
            var listening = CreateSampleListening(1, correctAnswer: "{\"1_q1\":[\"B\",\"A\"],\"1_q2\":\"B\"}");
            var listenings = new List<Listening> { listening };
            SetupMockRepository(listenings);

            var answers = new List<UserAnswerGroup>
            {
                new UserAnswerGroup
                {
                    SkillId = 1,
                    Answers = new Dictionary<string, object>
                    {
                        { "1_q1", new List<string> { "B","A"} },
                        {"1_q2", "B" }
                    }
                }
            };

            var result = _listeningService.EvaluateListening(1, answers);

            result.Should().Be(0m);
        }

        [Fact]
        public void EvaluateListening_CalculatesScoreForPlainTextAnswers()
        {
            var listening = CreateSampleListening(1, correctAnswer: "{\"1_q1\":[\"B\",\"A\"],\"1_q2\":\"B\"}");
            var listenings = new List<Listening> { listening };
            SetupMockRepository(listenings);
            var answers = new List<UserAnswerGroup>
            {
                new UserAnswerGroup
                {
                    SkillId = 1,
                    Answers = new Dictionary<string, object>
                    {
                        { "1_q1", new List<string> { "B","A"} },
                        {"1_q2", "A" }
                    }
                }
            };

            var result = _listeningService.EvaluateListening(1, answers);

            result.Should().Be(0m);
        }
    }
}
