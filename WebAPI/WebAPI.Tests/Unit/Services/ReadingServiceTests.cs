using FluentAssertions;
using Moq;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Repositories;
using WebAPI.Services;

namespace WebAPI.Tests
{
    public class ReadingServiceTests
    {
        private readonly Mock<IReadingRepository> _readingRepoMock;
        private readonly ReadingService _readingService;

        public ReadingServiceTests()
        {
            _readingRepoMock = new Mock<IReadingRepository>();
            _readingService = new ReadingService(_readingRepoMock.Object);
        }

        private Reading CreateSampleReading(int id = 1, int examId = 1, string correctAnswer = "answer")
        {
            return new Reading
            {
                ReadingId = id,
                ExamId = examId,
                ReadingContent = "Sample content",
                ReadingQuestion = "Sample question",
                ReadingType = "Markdown",
                DisplayOrder = 1,
                CorrectAnswer = correctAnswer,
                QuestionHtml = "<p>Sample</p>",
                CreatedAt = DateTime.UtcNow
            };
        }

        private void SetupMockRepository(List<Reading> initialReadings)
        {
            var nextId = initialReadings.Any()
                ? initialReadings.Max(r => r.ReadingId) + 1
                : 1;

            _readingRepoMock.Setup(repo => repo.GetById(It.IsAny<int>()))
                .Returns<int>(id => initialReadings.FirstOrDefault(r => r.ReadingId == id));

            _readingRepoMock.Setup(repo => repo.GetAll()).Returns(initialReadings);

            _readingRepoMock.Setup(repo => repo.GetByExamId(It.IsAny<int>()))
                .Returns<int>(examId =>
                    initialReadings.Where(r => r.ExamId == examId).ToList());

            _readingRepoMock.Setup(repo => repo.Add(It.IsAny<Reading>()))
                .Callback<Reading>(r =>
                {
                    if (r.ReadingId == 0)
                    {
                        r.ReadingId = nextId++;
                    }
                    initialReadings.Add(r);
                });

            _readingRepoMock.Setup(repo => repo.Update(It.IsAny<Reading>()))
                .Callback<Reading>(r =>
                {
                    var index = initialReadings.FindIndex(x => x.ReadingId == r.ReadingId);
                    if (index >= 0)
                    {
                        initialReadings[index] = r;
                    }
                });

            _readingRepoMock.Setup(repo => repo.Delete(It.IsAny<Reading>()))
                .Callback<Reading>(r =>
                {
                    initialReadings.RemoveAll(x => x.ReadingId == r.ReadingId);
                });

            _readingRepoMock.Setup(repo => repo.SaveChanges());
        }

        [Fact]
        public void GetById_WhenReadingExists_ReturnsDto()
        {
            var readings = new List<Reading> { CreateSampleReading(1), CreateSampleReading(2) };
            SetupMockRepository(readings);

            var result = _readingService.GetById(1);

            result.Should().NotBeNull();
            result!.ReadingId.Should().Be(1);
            result.ReadingContent.Should().Be("Sample content");
        }

        [Fact]
        public void GetById_WhenReadingNotFound_ReturnsNull()
        {
            var readings = new List<Reading>();
            SetupMockRepository(readings);

            var result = _readingService.GetById(5);

            result.Should().BeNull();
        }

        [Fact]
        public void GetReadingsByExam_ReturnsReadingsForExam()
        {
            var readings = new List<Reading>
            {
                CreateSampleReading(1, examId: 1),
                CreateSampleReading(2, examId: 2),
                CreateSampleReading(3, examId: 1)
            };
            SetupMockRepository(readings);

            var result = _readingService.GetReadingsByExam(1);

            result.Should().HaveCount(2).And.OnlyContain(r => r.ExamId == 1);
        }

        [Fact]
        public void GetAll_ReturnsAllReadingsAsDto()
        {
            var readings = new List<Reading> { CreateSampleReading(1), CreateSampleReading(2) };
            SetupMockRepository(readings);

            var result = _readingService.GetAll();

            result.Should().HaveCount(2);
            result.Select(r => r.ReadingId).Should().BeEquivalentTo(new[] { 1, 2 });
        }

        [Fact]
        public void Add_ValidDto_AddsReadingAndReturnsDto()
        {
            var readings = new List<Reading>();
            SetupMockRepository(readings);
            var dto = new CreateReadingDto
            {
                ExamId = 1,
                ReadingContent = "Content",
                ReadingQuestion = "Question",
                ReadingType = null,
                DisplayOrder = 1,
                CorrectAnswer = "answer",
                QuestionHtml = "<p>Sample</p>"
            };

            var result = _readingService.Add(dto);

            result.Should().NotBeNull();
            result!.ReadingId.Should().BeGreaterThan(0);
            result.ReadingType.Should().Be("Markdown");
            readings.Should().ContainSingle();
            readings[0].ReadingContent.Should().Be("Content");
        }

        [Fact]
        public void Update_WhenReadingExists_UpdatesPropertiesAndReturnsTrue()
        {
            var reading = CreateSampleReading(1);
            var readings = new List<Reading> { reading };
            SetupMockRepository(readings);
            var dto = new UpdateReadingDto
            {
                ReadingContent = "Updated content",
                ReadingQuestion = null,
                ReadingType = "HTML",
                DisplayOrder = 2,
                CorrectAnswer = "new answer",
                QuestionHtml = null
            };

            var result = _readingService.Update(1, dto);

            result.Should().BeTrue();
            reading.ReadingContent.Should().Be("Updated content");
            reading.ReadingType.Should().Be("HTML");
            reading.DisplayOrder.Should().Be(2);
            reading.CorrectAnswer.Should().Be("new answer");
        }

        [Fact]
        public void Update_WhenReadingNotFound_ReturnsFalse()
        {
            var readings = new List<Reading> { CreateSampleReading(1) };
            SetupMockRepository(readings);
            var dto = new UpdateReadingDto
            {
                ReadingContent = "Updated"
            };

            var result = _readingService.Update(99, dto);

            result.Should().BeFalse();
            readings.Should().HaveCount(1);
        }

        [Fact]
        public void Delete_WhenReadingExists_RemovesReadingAndReturnsTrue()
        {
            var reading = CreateSampleReading(1);
            var readings = new List<Reading> { reading };
            SetupMockRepository(readings);

            var result = _readingService.Delete(1);

            result.Should().BeTrue();
            readings.Should().BeEmpty();
        }

        [Fact]
        public void Delete_WhenReadingNotFound_ReturnsFalse()
        {
            var readings = new List<Reading> { CreateSampleReading(1) };
            SetupMockRepository(readings);

            var result = _readingService.Delete(2);

            result.Should().BeFalse();
            readings.Should().HaveCount(1);
        }

        [Fact]
        public void EvaluateReading_NoReadings_ReturnsZero()
        {
            var Readings = new List<Reading>();
            SetupMockRepository(Readings);

            var result = _readingService.EvaluateReading(1, new List<UserAnswerGroup>());

            result.Should().Be(0m);
        }

        [Fact]
        public void EvaluateReading_CalculatesScoreForPlainTextAnswers()
        {
            // Arrange
            var reading = CreateSampleReading(1, correctAnswer: "{\"1_q1\":[\"B\",\"A\"],\"1_q2\":\"B\"}");
            var readings = new List<Reading> { reading };
            SetupMockRepository(readings);

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

            // Act
            var result = _readingService.EvaluateReading(1, answers);

            // Assert
            result.Should().Be(6m);
        }


        [Fact]
        public void EvaluateReading_CalculatesScoreForJsonAnswers()
        {
            var reading = CreateSampleReading(1, correctAnswer: "{\"1_q1\":[\"B\",\"A\"],\"1_q2\":\"B\"}");
            var readings = new List<Reading> { reading };
            SetupMockRepository(readings);
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

            var result = _readingService.EvaluateReading(1, answers);

            result.Should().Be(9m);
        }




    }
}
