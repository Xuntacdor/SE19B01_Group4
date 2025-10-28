using System.Collections.Generic;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebAPI.Controllers;
using WebAPI.DTOs;
using WebAPI.Services;
using Xunit;

namespace WebAPI.Tests.Unit.Controller
{
    public class VipPlanControllerTests
    {
        private readonly Mock<IVipPlanService> _service;
        private readonly VipPlanController _controller;

        public VipPlanControllerTests()
        {
            _service = new Mock<IVipPlanService>();
            _controller = new VipPlanController(_service.Object);
        }

        [Fact]
        public void GetAll_ReturnsOk()
        {
            var plans = new List<VipPlanDTO> { new VipPlanDTO { VipPlanId = 1, PlanName = "A" } };
            _service.Setup(s => s.GetAll()).Returns(plans);

            var result = _controller.GetAll();

            result.Should().BeOfType<OkObjectResult>();
            var ok = result as OkObjectResult;
            ok!.Value.Should().BeEquivalentTo(plans);
        }

        [Fact]
        public void GetById_WhenFound_ReturnsOk()
        {
            var plan = new VipPlanDTO { VipPlanId = 2, PlanName = "B" };
            _service.Setup(s => s.GetById(2)).Returns(plan);

            var result = _controller.GetById(2);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void GetById_WhenNotFound_ReturnsNotFound()
        {
            _service.Setup(s => s.GetById(9)).Returns((VipPlanDTO?)null);

            var result = _controller.GetById(9);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Create_ReturnsCreated()
        {
            var dto = new VipPlanDTO { PlanName = "C" };
            var created = new VipPlanDTO { VipPlanId = 3, PlanName = "C" };
            _service.Setup(s => s.Create(dto)).Returns(created);

            var result = _controller.Create(dto);

            result.Should().BeOfType<CreatedAtActionResult>();
            var createdResult = result as CreatedAtActionResult;
            createdResult!.Value.Should().BeEquivalentTo(created);
        }

        [Fact]
        public void Update_WhenFound_ReturnsOk()
        {
            var dto = new VipPlanDTO { PlanName = "D" };
            var updated = new VipPlanDTO { VipPlanId = 4, PlanName = "D" };
            _service.Setup(s => s.Update(4, dto)).Returns(updated);

            var result = _controller.Update(4, dto);

            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public void Update_WhenNotFound_ReturnsNotFound()
        {
            var dto = new VipPlanDTO { PlanName = "E" };
            _service.Setup(s => s.Update(5, dto)).Returns((VipPlanDTO?)null);

            var result = _controller.Update(5, dto);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public void Delete_WhenSuccess_ReturnsNoContent()
        {
            _service.Setup(s => s.Delete(6)).Returns(true);

            var result = _controller.Delete(6);

            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public void Delete_WhenNotFound_ReturnsNotFound()
        {
            _service.Setup(s => s.Delete(7)).Returns(false);

            var result = _controller.Delete(7);

            result.Should().BeOfType<NotFoundResult>();
        }
    }
}


