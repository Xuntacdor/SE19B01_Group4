using Xunit;
using Moq;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using WebAPI.Services;
using WebAPI.Repositories;
using WebAPI.ExternalServices;
using WebAPI.DTOs;
using WebAPI.Models;

namespace WebAPI.Tests
{
    public class WritingServiceTests
    {
        private readonly Mock<IWritingRepository> _writingRepoMock;
        private readonly Mock<IWritingFeedbackRepository> _feedbackRepoMock;
        private readonly Mock<IOpenAIService> _openAIMock;
        private readonly Mock<IExamService> _examServiceMock;
        private readonly WritingService _service;

        public WritingServiceTests()
        {
            _writingRepoMock = new Mock<IWritingRepository>();
            _feedbackRepoMock = new Mock<IWritingFeedbackRepository>();
            _openAIMock = new Mock<IOpenAIService>();
            _examServiceMock = new Mock<IExamService>();
            _service = new WritingService(_writingRepoMock.Object, _feedbackRepoMock.Object, _openAIMock.Object, _examServiceMock.Object);
        }

        private static JsonDocument CreateSampleFeedback()
        {
            var json = @"{
  ""band_estimate"": {
    ""task_achievement"": 7,
    ""organization_logic"": 7,
    ""lexical_resource"": 7,
    ""grammar_accuracy"": 7,
    ""overall"": 7
  },
  ""grammar_vocab"": {
    ""overview"": ""good""
  },
  ""overall_feedback"": {
    ""overview"": ""good""
  }
}";
            return JsonDocument.Parse(json);
        }

        private void SetupSaveFeedback()
        {
            _examServiceMock.Setup(s => s.GetExamAttemptsByUser(It.IsAny<int>()))
                .Returns(new List<ExamAttemptSummaryDto>());
            _examServiceMock.Setup(s => s.SubmitAttempt(It.IsAny<SubmitAttemptDto>(), It.IsAny<int>()))
                .Returns(new ExamAttempt { AttemptId = 1, AnswerText = null });
            _examServiceMock.Setup(s => s.GetAttemptById(It.IsAny<int>())).Returns((ExamAttempt)null);
            _examServiceMock.Setup(s => s.Save());
            _feedbackRepoMock.Setup(f => f.GetAll()).Returns(new List<WritingFeedback>());
        }

        [Fact]
        public void GivenExistingId_WhenGetById_ThenReturnsDto()
        {
            var entity = new Writing { WritingId = 1, ExamId = 2, WritingQuestion = "Test", DisplayOrder = 1, CreatedAt = DateTime.UtcNow, ImageUrl = "img" };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(entity);

            var result = _service.GetById(1);

            result.Should().NotBeNull();
            result!.WritingId.Should().Be(1);
            result.ExamId.Should().Be(2);
            result.WritingQuestion.Should().Be("Test");
        }

        [Fact]
        public void GivenMissingId_WhenGetById_ThenReturnsNull()
        {
            _writingRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Writing)null);

            var result = _service.GetById(99);

            result.Should().BeNull();
        }

        [Fact]
        public void GivenRepositoryThrows_WhenGetById_ThenThrows()
        {
            _writingRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Throws(new Exception("failure"));

            Action act = () => _service.GetById(1);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenCall_WhenGetById_ThenRepositoryCalledOnce()
        {
            _writingRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns(new Writing { WritingId = 1 });

            _service.GetById(1);

            _writingRepoMock.Verify(r => r.GetById(1), Times.Once);
        }

        [Fact]
        public void GivenExamWithWritings_WhenGetByExam_ThenReturnsDtosList()
        {
            var examId = 1;
            var writings = new List<Writing>
            {
                new Writing { WritingId = 1, ExamId = examId, WritingQuestion = "Q1", DisplayOrder = 1, CreatedAt = DateTime.UtcNow, ImageUrl = null },
                new Writing { WritingId = 2, ExamId = examId, WritingQuestion = "Q2", DisplayOrder = 2, CreatedAt = DateTime.UtcNow, ImageUrl = null }
            };
            _writingRepoMock.Setup(r => r.GetByExamId(examId)).Returns(writings);

            var result = _service.GetByExam(examId);

            result.Should().HaveCount(2);
            result.Should().OnlyContain(d => d.ExamId == examId);
        }

        [Fact]
        public void GivenExamWithoutWritings_WhenGetByExam_ThenReturnsEmptyList()
        {
            _writingRepoMock.Setup(r => r.GetByExamId(It.IsAny<int>())).Returns(new List<Writing>());

            var result = _service.GetByExam(1);

            result.Should().BeEmpty();
        }

        [Fact]
        public void GivenRepositoryThrows_WhenGetByExam_ThenThrows()
        {
            _writingRepoMock.Setup(r => r.GetByExamId(It.IsAny<int>())).Throws(new Exception("failure"));

            Action act = () => _service.GetByExam(1);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenCall_WhenGetByExam_ThenRepositoryInvoked()
        {
            _writingRepoMock.Setup(r => r.GetByExamId(It.IsAny<int>())).Returns(new List<Writing>());

            _service.GetByExam(3);

            _writingRepoMock.Verify(r => r.GetByExamId(3), Times.Once);
        }

        [Fact]
        public void GivenValidDto_WhenCreate_ThenReturnsDtoWithId()
        {
            var dto = new WritingDTO { ExamId = 1, WritingQuestion = "Q", DisplayOrder = 1, ImageUrl = "img" };
            _writingRepoMock.Setup(r => r.Add(It.IsAny<Writing>())).Callback<Writing>(w => w.WritingId = 10);
            _writingRepoMock.Setup(r => r.SaveChanges());

            var result = _service.Create(dto);

            result.Should().NotBeNull();
            result.WritingId.Should().Be(10);
            result.WritingQuestion.Should().Be("Q");
        }

        [Fact]
        public void GivenDtoWithoutImage_WhenCreate_ThenCreatesWithNullImage()
        {
            var dto = new WritingDTO { ExamId = 1, WritingQuestion = "Q", DisplayOrder = 1, ImageUrl = null };
            _writingRepoMock.Setup(r => r.Add(It.IsAny<Writing>())).Callback<Writing>(w => w.WritingId = 5);
            _writingRepoMock.Setup(r => r.SaveChanges());

            var result = _service.Create(dto);

            result.ImageUrl.Should().BeNull();
        }

        [Fact]
        public void GivenRepoThrows_WhenCreate_ThenThrows()
        {
            var dto = new WritingDTO { ExamId = 1, WritingQuestion = "Q", DisplayOrder = 1 };
            _writingRepoMock.Setup(r => r.Add(It.IsAny<Writing>())).Throws(new Exception("failure"));

            Action act = () => _service.Create(dto);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenValidDto_WhenCreate_ThenCallsAddAndSaveChanges()
        {
            var dto = new WritingDTO { ExamId = 1, WritingQuestion = "Q", DisplayOrder = 1 };
            _writingRepoMock.Setup(r => r.Add(It.IsAny<Writing>())).Callback<Writing>(w => w.WritingId = 3);
            _writingRepoMock.Setup(r => r.SaveChanges());

            _service.Create(dto);

            _writingRepoMock.Verify(r => r.Add(It.IsAny<Writing>()), Times.Once);
            _writingRepoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void GivenIdExistsAndDtoWithNewValues_WhenUpdate_ThenReturnsUpdatedDto()
        {
            var existing = new Writing { WritingId = 1, WritingQuestion = "Old", DisplayOrder = 1, ImageUrl = "oldimg" };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(existing);
            _writingRepoMock.Setup(r => r.Update(It.IsAny<Writing>()));
            _writingRepoMock.Setup(r => r.SaveChanges());
            var dto = new WritingDTO { WritingQuestion = "New", DisplayOrder = 2, ImageUrl = "newimg" };

            var result = _service.Update(1, dto);

            result.Should().NotBeNull();
            result!.WritingQuestion.Should().Be("New");
            result.DisplayOrder.Should().Be(2);
            result.ImageUrl.Should().Be("newimg");
        }

        [Fact]
        public void GivenDtoMissingFields_WhenUpdate_ThenRetainsExistingFields()
        {
            var existing = new Writing { WritingId = 1, WritingQuestion = "Old", DisplayOrder = 3, ImageUrl = "img" };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(existing);
            _writingRepoMock.Setup(r => r.Update(It.IsAny<Writing>()));
            _writingRepoMock.Setup(r => r.SaveChanges());
            var dto = new WritingDTO { WritingQuestion = null, DisplayOrder = 0, ImageUrl = null };

            var result = _service.Update(1, dto);

            result.WritingQuestion.Should().Be("Old");
            result.DisplayOrder.Should().Be(3);
            result.ImageUrl.Should().Be("img");
        }

        [Fact]
        public void GivenIdMissing_WhenUpdate_ThenReturnsNull()
        {
            _writingRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Writing)null);

            var result = _service.Update(99, new WritingDTO());

            result.Should().BeNull();
        }

        [Fact]
        public void GivenRepositoryThrows_WhenUpdate_ThenThrows()
        {
            var existing = new Writing { WritingId = 1 };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(existing);
            _writingRepoMock.Setup(r => r.Update(It.IsAny<Writing>())).Throws(new Exception("failure"));

            Action act = () => _service.Update(1, new WritingDTO { WritingQuestion = "X", DisplayOrder = 1 });

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenIdExists_WhenUpdate_ThenCallsUpdateAndSaveChanges()
        {
            var existing = new Writing { WritingId = 1 };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(existing);
            _writingRepoMock.Setup(r => r.Update(It.IsAny<Writing>()));
            _writingRepoMock.Setup(r => r.SaveChanges());

            _service.Update(1, new WritingDTO { WritingQuestion = "Q", DisplayOrder = 1 });

            _writingRepoMock.Verify(r => r.Update(existing), Times.Once);
            _writingRepoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void GivenExistingId_WhenDelete_ThenRemovesWriting()
        {
            var existing = new Writing { WritingId = 1 };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(existing);
            _writingRepoMock.Setup(r => r.Delete(existing));
            _writingRepoMock.Setup(r => r.SaveChanges());

            var result = _service.Delete(1);

            result.Should().BeTrue();
        }

        [Fact]
        public void GivenMissingId_WhenDelete_ThenReturnsFalse()
        {
            _writingRepoMock.Setup(r => r.GetById(It.IsAny<int>())).Returns((Writing)null);

            var result = _service.Delete(99);

            result.Should().BeFalse();
        }

        [Fact]
        public void GivenRepositoryThrows_WhenDelete_ThenThrows()
        {
            var existing = new Writing { WritingId = 1 };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(existing);
            _writingRepoMock.Setup(r => r.Delete(existing)).Throws(new Exception("failure"));

            Action act = () => _service.Delete(1);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenExistingId_WhenDelete_ThenCallsDeleteAndSaveChanges()
        {
            var existing = new Writing { WritingId = 1 };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(existing);
            _writingRepoMock.Setup(r => r.Delete(existing));
            _writingRepoMock.Setup(r => r.SaveChanges());

            _service.Delete(1);

            _writingRepoMock.Verify(r => r.Delete(existing), Times.Once);
            _writingRepoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void GivenSingleMode_WhenGradeWriting_ThenReturnsSingleJson()
        {
            SetupSaveFeedback();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new List<WritingAnswerDTO>
                {
                    new WritingAnswerDTO { WritingId = 1, AnswerText = "Answer1", ImageUrl = null, DisplayOrder = 1 }
                }
            };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(new Writing { WritingId = 1, WritingQuestion = "Question1" });
            var sample = CreateSampleFeedback();
            _openAIMock.Setup(s => s.GradeWriting("Question1", "Answer1", null)).Returns(sample);

            var result = _service.GradeWriting(dto, 2);

            result.Should().BeSameAs(sample);
            _openAIMock.Verify(s => s.GradeWriting("Question1", "Answer1", null), Times.Once);
        }

        [Fact]
        public void GivenFullMode_WhenGradeWriting_ThenAggregatesResults()
        {
            SetupSaveFeedback();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "full",
                ExamId = 1,
                Answers = new List<WritingAnswerDTO>
                {
                    new WritingAnswerDTO { WritingId = 1, AnswerText = "A1", ImageUrl = null, DisplayOrder = 1 },
                    new WritingAnswerDTO { WritingId = 2, AnswerText = "A2", ImageUrl = null, DisplayOrder = 2 }
                }
            };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(new Writing { WritingId = 1, WritingQuestion = "Q1" });
            _writingRepoMock.Setup(r => r.GetById(2)).Returns(new Writing { WritingId = 2, WritingQuestion = "Q2" });
            var sample = CreateSampleFeedback();
            _openAIMock.Setup(s => s.GradeWriting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(sample);

            var result = _service.GradeWriting(dto, 2);

            var root = result.RootElement;
            root.GetProperty("examId").GetInt32().Should().Be(1);
            root.GetProperty("totalAnswers").GetInt32().Should().Be(2);
            root.GetProperty("feedbacks").EnumerateArray().Count().Should().Be(2);
            _openAIMock.Verify(s => s.GradeWriting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void GivenUnknownMode_WhenGradeWriting_ThenDefaultsToFull()
        {
            SetupSaveFeedback();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "unknown",
                ExamId = 1,
                Answers = new List<WritingAnswerDTO>
                {
                    new WritingAnswerDTO { WritingId = 1, AnswerText = "A1", ImageUrl = null, DisplayOrder = 1 },
                    new WritingAnswerDTO { WritingId = 2, AnswerText = "A2", ImageUrl = null, DisplayOrder = 2 }
                }
            };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(new Writing { WritingId = 1, WritingQuestion = "Q1" });
            _writingRepoMock.Setup(r => r.GetById(2)).Returns(new Writing { WritingId = 2, WritingQuestion = "Q2" });
            var sample = CreateSampleFeedback();
            _openAIMock.Setup(s => s.GradeWriting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(sample);

            var result = _service.GradeWriting(dto, 2);

            result.RootElement.GetProperty("totalAnswers").GetInt32().Should().Be(2);
            _openAIMock.Verify(s => s.GradeWriting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
        }

        [Fact]
        public void GivenAIThrows_WhenGradeWriting_ThenThrows()
        {
            SetupSaveFeedback();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new List<WritingAnswerDTO>
                {
                    new WritingAnswerDTO { WritingId = 1, AnswerText = "A", ImageUrl = null, DisplayOrder = 1 }
                }
            };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(new Writing { WritingId = 1, WritingQuestion = "Q" });
            _openAIMock.Setup(s => s.GradeWriting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Throws(new Exception("AI error"));

            Action act = () => _service.GradeWriting(dto, 2);

            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GivenMultipleAnswers_WhenGradeWriting_ThenInvokesAIForEachAndSavesFeedback()
        {
            SetupSaveFeedback();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "full",
                ExamId = 1,
                Answers = new List<WritingAnswerDTO>
                {
                    new WritingAnswerDTO { WritingId = 1, AnswerText = "A1", ImageUrl = null, DisplayOrder = 1 },
                    new WritingAnswerDTO { WritingId = 2, AnswerText = "A2", ImageUrl = null, DisplayOrder = 2 },
                    new WritingAnswerDTO { WritingId = 3, AnswerText = "A3", ImageUrl = null, DisplayOrder = 3 }
                }
            };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(new Writing { WritingId = 1, WritingQuestion = "Q1" });
            _writingRepoMock.Setup(r => r.GetById(2)).Returns(new Writing { WritingId = 2, WritingQuestion = "Q2" });
            _writingRepoMock.Setup(r => r.GetById(3)).Returns(new Writing { WritingId = 3, WritingQuestion = "Q3" });
            var sample = CreateSampleFeedback();
            _openAIMock.Setup(s => s.GradeWriting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(sample);
            _feedbackRepoMock.Setup(f => f.SaveChanges());

            _service.GradeWriting(dto, 2);

            _openAIMock.Verify(s => s.GradeWriting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(dto.Answers.Count));
            _feedbackRepoMock.Verify(f => f.SaveChanges(), Times.Exactly(dto.Answers.Count));
        }
    }
}
