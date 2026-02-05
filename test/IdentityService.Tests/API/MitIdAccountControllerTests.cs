using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityService.Application.BusinessLogicLayer.Interface;
using IdentityService.Application.DTOs;
using IdentityService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace IdentityService.Tests.API
{
    public class MitIdAccountControllerTests
    {
        private readonly Mock<IMitIdAccountService> _serviceMock;
        private readonly MitIdAccountsController _sut;

        public MitIdAccountControllerTests()
        {
            _serviceMock = new Mock<IMitIdAccountService>();
            _sut = new MitIdAccountsController(_serviceMock.Object);
        }

        [Fact]
        public async Task GetByAccountId_WhenFound_ReturnsOkWithDto()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new MitIdAccountDto { AccountId = id };

            _serviceMock
                .Setup(s => s.GetByAccountIdAsync(id))
                .ReturnsAsync(dto);

            // Act
            var result = await _sut.GetByAccountId(id);

            // Assert
            var actionResult = Assert.IsType<ActionResult<MitIdAccountDto>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedDto = Assert.IsType<MitIdAccountDto>(okResult.Value);
            Assert.Equal(id, returnedDto.AccountId);
        }

        [Fact]
        public async Task GetByAccountId_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();

            _serviceMock
                .Setup(s => s.GetByAccountIdAsync(id))
                .ReturnsAsync((MitIdAccountDto?)null);

            // Act
            var result = await _sut.GetByAccountId(id);

            // Assert
            var actionResult = Assert.IsType<ActionResult<MitIdAccountDto>>(result);
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetAll_ReturnsOkWithList()
        {
            // Arrange
            var list = new List<MitIdAccountDto>
            {
                new MitIdAccountDto { AccountId = Guid.NewGuid() },
                new MitIdAccountDto { AccountId = Guid.NewGuid() }
            };

            _serviceMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync(list);

            // Act
            var result = await _sut.GetAll();

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<MitIdAccountDto>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedList = Assert.IsType<List<MitIdAccountDto>>(okResult.Value);
            Assert.Equal(2, returnedList.Count);
        }

        [Fact]
        public async Task Update_WhenServiceReturnsTrue_ReturnsNoContent()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new MitIdAccountDto { AccountId = id };

            _serviceMock
                .Setup(s => s.UpdateAsync(id, dto))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.Update(id, dto);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Update_WhenServiceReturnsFalse_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new MitIdAccountDto { AccountId = id };

            _serviceMock
                .Setup(s => s.UpdateAsync(id, dto))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.Update(id, dto);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_WhenServiceReturnsTrue_ReturnsNoContent()
        {
            // Arrange
            var id = Guid.NewGuid();

            _serviceMock
                .Setup(s => s.DeleteAsync(id))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.Delete(id);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_WhenServiceReturnsFalse_ReturnsNotFound()
        {
            // Arrange
            var id = Guid.NewGuid();

            _serviceMock
                .Setup(s => s.DeleteAsync(id))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.Delete(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }
    }
}
