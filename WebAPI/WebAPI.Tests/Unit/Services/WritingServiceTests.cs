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
              ""grammar_vocab"": { ""overview"": ""good"" },
              ""overall_feedback"": { ""overview"": ""good"" }
            }";
            return JsonDocument.Parse(json);
        }

        private void SetupSaveFeedback()
        {
            _examServiceMock.Setup(s => s.GetExamAttemptsByUser(It.IsAny<int>()))
                .Returns(() => new List<ExamAttemptSummaryDto>());
            _examServiceMock.Setup(s => s.SubmitAttempt(It.IsAny<SubmitAttemptDto>(), It.IsAny<int>()))
                .Returns(() => new ExamAttempt { AttemptId = 1, AnswerText = null });
            _examServiceMock.Setup(s => s.GetAttemptById(It.IsAny<int>()))
                .Returns(() => null);
            _examServiceMock.Setup(s => s.Save());
            _feedbackRepoMock.Setup(f => f.GetAll()).Returns(() => new List<WritingFeedback>());
        }

        // ---------------------------------------------------------------------
        // CRUD
        // ---------------------------------------------------------------------

        [Fact]
        public void GetById_WhenFound_ReturnsDto()
        {
            var entity = new Writing { WritingId = 1, ExamId = 2, WritingQuestion = "Test", DisplayOrder = 1, ImageUrl = "img" };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => entity);

            var result = _service.GetById(1);

            result.Should().NotBeNull();
            result.WritingId.Should().Be(1);
            result.ExamId.Should().Be(2);
        }

        [Fact]
        public void GetById_WhenMissing_ReturnsNull()
        {
            _writingRepoMock.Setup(r => r.GetById(99)).Returns(() => null);
            _service.GetById(99).Should().BeNull();
        }

        [Fact]
        public void GetById_WhenThrows_Rethrows()
        {
            _writingRepoMock.Setup(r => r.GetById(1)).Throws(new Exception("fail"));
            Action act = () => _service.GetById(1);
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void GetByExam_ReturnsList()
        {
            var writings = new List<Writing> { new() { WritingId = 1, ExamId = 1 }, new() { WritingId = 2, ExamId = 1 } };
            _writingRepoMock.Setup(r => r.GetByExamId(1)).Returns(() => writings);

            var result = _service.GetByExam(1);
            result.Should().HaveCount(2);
        }

        [Fact]
        public void GetByExam_WhenEmpty_ReturnsEmpty()
        {
            _writingRepoMock.Setup(r => r.GetByExamId(2)).Returns(() => new List<Writing>());
            _service.GetByExam(2).Should().BeEmpty();
        }

        [Fact]
        public void Create_WhenValid_ReturnsDto()
        {
            var dto = new WritingDTO { ExamId = 1, WritingQuestion = "Q", DisplayOrder = 1 };
            _writingRepoMock.Setup(r => r.Add(It.IsAny<Writing>())).Callback<Writing>(w => w.WritingId = 11);

            var result = _service.Create(dto);
            result.WritingId.Should().Be(11);
            _writingRepoMock.Verify(r => r.SaveChanges(), Times.Once);
        }

        [Fact]
        public void Update_WhenFound_UpdatesFields()
        {
            var entity = new Writing { WritingId = 1, WritingQuestion = "Old" };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => entity);

            var dto = new WritingDTO { WritingQuestion = "New", DisplayOrder = 3, ImageUrl = "i" };
            var result = _service.Update(1, dto);

            result!.WritingQuestion.Should().Be("New");
            _writingRepoMock.Verify(r => r.Update(entity), Times.Once);
        }

        [Fact]
        public void Update_WhenMissing_ReturnsNull()
        {
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => null);
            _service.Update(1, new WritingDTO()).Should().BeNull();
        }

        [Fact]
        public void Delete_WhenFound_ReturnsTrue()
        {
            var entity = new Writing { WritingId = 1 };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => entity);
            _service.Delete(1).Should().BeTrue();
            _writingRepoMock.Verify(r => r.Delete(entity), Times.Once);
        }

        [Fact]
        public void Delete_WhenMissing_ReturnsFalse()
        {
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => null);
            _service.Delete(1).Should().BeFalse();
        }

        // ---------------------------------------------------------------------
        // GRADE WRITING
        // ---------------------------------------------------------------------

        [Fact]
        public void GradeWriting_SingleMode_ReturnsJson()
        {
            SetupSaveFeedback();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new() { new() { WritingId = 1, AnswerText = "Ans" } }
            };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => new Writing { WritingId = 1, WritingQuestion = "Q" });
            var sample = CreateSampleFeedback();
            _openAIMock.Setup(s => s.GradeWriting("Q", "Ans", null)).Returns(() => sample);

            var result = _service.GradeWriting(dto, 1);
            result.Should().BeSameAs(sample);
        }

        [Fact]
        public void GradeWriting_FullMode_ReturnsAggregated()
        {
            SetupSaveFeedback();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "full",
                ExamId = 1,
                Answers = new()
                {
                    new() { WritingId = 1, AnswerText = "A1" },
                    new() { WritingId = 2, AnswerText = "A2" }
                }
            };
            _writingRepoMock.Setup(r => r.GetById(It.IsAny<int>()))
                .Returns<int>(id => new Writing { WritingId = id, WritingQuestion = $"Q{id}" });
            _openAIMock.Setup(s => s.GradeWriting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => CreateSampleFeedback());

            var result = _service.GradeWriting(dto, 1);
            result.RootElement.GetProperty("totalAnswers").GetInt32().Should().Be(2);
        }

        [Fact]
        public void GradeWriting_UnknownMode_DefaultsToFull()
        {
            SetupSaveFeedback();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "weird",
                ExamId = 1,
                Answers = new()
                {
                    new() { WritingId = 1, AnswerText = "A1" },
                    new() { WritingId = 2, AnswerText = "A2" }
                }
            };
            _writingRepoMock.Setup(r => r.GetById(It.IsAny<int>()))
                .Returns<int>(id => new Writing { WritingId = id, WritingQuestion = $"Q{id}" });
            _openAIMock.Setup(s => s.GradeWriting(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(() => CreateSampleFeedback());

            var result = _service.GradeWriting(dto, 2);
            result.RootElement.GetProperty("totalAnswers").GetInt32().Should().Be(2);
        }

        [Fact]
        public void GradeWriting_WhenAIThrows_Rethrows()
        {
            SetupSaveFeedback();
            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new() { new() { WritingId = 1, AnswerText = "Ans" } }
            };
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => new Writing { WritingId = 1, WritingQuestion = "Q" });
            _openAIMock.Setup(s => s.GradeWriting("Q", "Ans", null)).Throws(new Exception("AI error"));
            Action act = () => _service.GradeWriting(dto, 1);
            act.Should().Throw<Exception>();
        }

        // ---------------------------------------------------------------------
        // SAVE FEEDBACK
        // ---------------------------------------------------------------------

        [Fact]
        public void SaveFeedback_WhenExamAttemptNotFound_HandledGracefully()
        {
            var feedback = CreateSampleFeedback();
            var writing = new Writing { WritingId = 1, WritingQuestion = "Q" };
            var summary = new ExamAttemptSummaryDto { ExamId = 1, AttemptId = 99 };
            _examServiceMock.Setup(s => s.GetExamAttemptsByUser(1))
                .Returns(() => new List<ExamAttemptSummaryDto> { summary });
            _examServiceMock.Setup(s => s.GetAttemptById(99)).Returns(() => null);
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => writing);
            _openAIMock.Setup(s => s.GradeWriting("Q", "Ans", null)).Returns(() => feedback);

            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new() { new() { WritingId = 1, AnswerText = "Ans" } }
            };

            Action act = () => _service.GradeWriting(dto, 1);
            act.Should().NotThrow();
        }

        [Fact]
        public void SaveFeedback_WhenAttemptHasAnswerText_StillSavesAccordingToServiceLogic()
        {
            // logic mới: attempt có answerText nhưng service vẫn gọi Save()
            var feedback = CreateSampleFeedback();
            var writing = new Writing { WritingId = 1, WritingQuestion = "Q" };
            var summary = new ExamAttemptSummaryDto { ExamId = 1, AttemptId = 5 };
            var attempt = new ExamAttempt { AttemptId = 5, ExamId = 1, AnswerText = "exists" };

            _examServiceMock.Setup(s => s.GetExamAttemptsByUser(1)).Returns(() => new() { summary });
            _examServiceMock.Setup(s => s.GetAttemptById(5)).Returns(() => attempt);
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => writing);
            _openAIMock.Setup(s => s.GradeWriting("Q", "Ans", null)).Returns(() => feedback);
            _feedbackRepoMock.Setup(f => f.GetByAttemptAndWriting(5, 1)).Returns(() => null);

            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new() { new() { WritingId = 1, AnswerText = "Ans" } }
            };

            _service.GradeWriting(dto, 1);

            _examServiceMock.Verify(s => s.Save(), Times.Once);
        }

        [Fact]
        public void SaveFeedback_WhenAttemptAnswerTextEmpty_SetsAndSaves()
        {
            var feedback = CreateSampleFeedback();
            var writing = new Writing { WritingId = 1, WritingQuestion = "Q" };
            var summary = new ExamAttemptSummaryDto { ExamId = 1, AttemptId = 6 };
            var attempt = new ExamAttempt { AttemptId = 6, ExamId = 1, AnswerText = null };

            _examServiceMock.Setup(s => s.GetExamAttemptsByUser(1)).Returns(() => new() { summary });
            _examServiceMock.Setup(s => s.GetAttemptById(6)).Returns(() => attempt);
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => writing);
            _openAIMock.Setup(s => s.GradeWriting("Q", "Ans", null)).Returns(() => feedback);
            _feedbackRepoMock.Setup(f => f.GetByAttemptAndWriting(6, 1)).Returns(() => null);
            _feedbackRepoMock.Setup(f => f.Add(It.IsAny<WritingFeedback>()));

            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new() { new() { WritingId = 1, AnswerText = "Ans" } }
            };

            _service.GradeWriting(dto, 1);

            _examServiceMock.Verify(s => s.Save(), Times.Once);
            attempt.AnswerText.Should().Be("--- TASK 1 ---\nAns");
        }

        [Fact]
        public void SaveFeedback_WhenNewFeedback_AddsEntity()
        {
            var feedback = CreateSampleFeedback();
            var writing = new Writing { WritingId = 1, WritingQuestion = "Q" };
            var summary = new ExamAttemptSummaryDto { ExamId = 1, AttemptId = 8 };
            var attempt = new ExamAttempt { AttemptId = 8, ExamId = 1, AnswerText = "Ans" };

            _examServiceMock.Setup(s => s.GetExamAttemptsByUser(1)).Returns(() => new() { summary });
            _examServiceMock.Setup(s => s.GetAttemptById(8)).Returns(() => attempt);
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => writing);
            _openAIMock.Setup(s => s.GradeWriting("Q", "Ans", null)).Returns(() => feedback);
            _feedbackRepoMock.Setup(f => f.GetByAttemptAndWriting(8, 1)).Returns(() => null);

            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new() { new() { WritingId = 1, AnswerText = "Ans" } }
            };

            _service.GradeWriting(dto, 1);

            _feedbackRepoMock.Verify(f => f.Add(It.Is<WritingFeedback>(fb => fb.AttemptId == 8)), Times.Once);
        }

        [Fact]
        public void SaveFeedback_WhenExceptionThrown_HandledByCatch()
        {
            var feedback = CreateSampleFeedback();
            var writing = new Writing { WritingId = 1, WritingQuestion = "Q" };
            _examServiceMock.Setup(s => s.GetExamAttemptsByUser(1)).Throws(new Exception("boom"));
            _writingRepoMock.Setup(r => r.GetById(1)).Returns(() => writing);
            _openAIMock.Setup(s => s.GradeWriting("Q", "Ans", null)).Returns(() => feedback);

            var dto = new WritingGradeRequestDTO
            {
                Mode = "single",
                ExamId = 1,
                Answers = new() { new() { WritingId = 1, AnswerText = "Ans" } }
            };

            Action act = () => _service.GradeWriting(dto, 1);
            act.Should().NotThrow();
        }
    }
}
