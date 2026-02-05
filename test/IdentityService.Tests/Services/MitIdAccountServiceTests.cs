using AutoMapper;
using IdentityService.Application.BusinessLogicLayer;
using IdentityService.Application.BusinessLogicLayer.Interface;
using IdentityService.Application.DTOs;
using IdentityService.Application.Security;
using IdentityService.Domain.Interfaces;
using IdentityService.Domain.Model;
using Moq;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace IdentityService.Tests.Services
{
    public class MitIdAccountServiceTests
    {
        private readonly Mock<IMitIdDbAccess> _dbMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IMitIdAccountCache> _cacheMock;
        private readonly Mock<IAccDbAccess> _accDbMock;
        private readonly MitIdAccountService _sut;

        public MitIdAccountServiceTests()
        {
            _dbMock = new Mock<IMitIdDbAccess>();
            _mapperMock = new Mock<IMapper>();
            _cacheMock = new Mock<IMitIdAccountCache>();
            _accDbMock = new Mock<IAccDbAccess>();

            _sut = new MitIdAccountService(
                _dbMock.Object,
                _accDbMock.Object,
                _mapperMock.Object,
                _cacheMock.Object);
        }

        // ---------- Helpers ----------

        private static ClaimsPrincipal CreateUserWithClaims(
            string? sub = null,
            string? dob = null)
        {
            var claims = new List<Claim>();

            if (sub != null)
            {
                // MitID kan bruge enten NameIdentifier eller "sub"
                claims.Add(new Claim(ClaimTypes.NameIdentifier, sub));
            }

            if (dob != null)
            {
                claims.Add(new Claim(
                    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth",
                    dob));
            }

            var identity = new ClaimsIdentity(claims, "test");
            return new ClaimsPrincipal(identity);
        }

        // ---------- CreateFromClaimsAsync tests ----------

        [Fact]
        public async Task CreateFromClaimsAsync_WhenNoSub_ReturnsNull()
        {
            // Arrange
            var user = CreateUserWithClaims(sub: null, dob: "1990-01-01");
            var accountId = Guid.NewGuid();

            // Act
            var result = await _sut.CreateFromClaimsAsync(user, accountId);

            // Assert
            Assert.Null(result);
            _dbMock.Verify(d => d.GetMitIdAccountBySubId(It.IsAny<string>()), Times.Never);
            _accDbMock.Verify(a => a.GetAccountByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_WhenNoDobClaim_ReturnsNull()
        {
            // Arrange
            var user = CreateUserWithClaims(sub: "12345", dob: null);
            var accountId = Guid.NewGuid();

            // Act
            var result = await _sut.CreateFromClaimsAsync(user, accountId);

            // Assert
            Assert.Null(result);
            _dbMock.Verify(d => d.GetMitIdAccountBySubId(It.IsAny<string>()), Times.Never);
            _accDbMock.Verify(a => a.GetAccountByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_WhenDobInvalid_ReturnsNull()
        {
            // Arrange
            var user = CreateUserWithClaims(sub: "12345", dob: "not-a-date");
            var accountId = Guid.NewGuid();

            // Act
            var result = await _sut.CreateFromClaimsAsync(user, accountId);

            // Assert
            Assert.Null(result);
            _dbMock.Verify(d => d.GetMitIdAccountBySubId(It.IsAny<string>()), Times.Never);
            _accDbMock.Verify(a => a.GetAccountByIdAsync(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_WhenExistingAccount_ReturnsExistingAndCaches()
        {
            // Arrange
            var sub = "mitid-user-1";
            var hashedSub = SubIdHasher.Hash(sub);
            var user = CreateUserWithClaims(sub, "1990-01-01");
            var accountId = Guid.NewGuid();

            var entity = new MitID_Account
            {
                ID = Guid.NewGuid(),
                AccountID = Guid.NewGuid(),
                SubID = hashedSub,
                IsAdult = true
            };

            var dto = new MitIdAccountDto
            {
                Id = entity.ID,
                AccountId = entity.AccountID,
                SubId = entity.SubID,
                IsAdult = entity.IsAdult
            };

            _dbMock
                .Setup(d => d.GetMitIdAccountBySubId(hashedSub))
                .ReturnsAsync(entity);

            _mapperMock
                .Setup(m => m.Map<MitIdAccountDto>(entity))
                .Returns(dto);

            // Act
            var result = await _sut.CreateFromClaimsAsync(user, accountId);

            // Assert
            Assert.NotNull(result);
            Assert.False(result!.IsNew);
            Assert.Equal(dto, result.Account);

            _dbMock.Verify(d => d.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
            _accDbMock.Verify(a => a.GetAccountByIdAsync(It.IsAny<Guid>()), Times.Never); // new-path only

            _cacheMock.Verify(c => c.SetAsync(
                    dto,
                    It.IsAny<TimeSpan>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_WhenParentAccountMissing_ReturnsNull_AndDoesNotCreate()
        {
            // Arrange
            var sub = "new-mitid-user";
            var hashedSub = SubIdHasher.Hash(sub);
            var user = CreateUserWithClaims(sub, "1990-01-01");
            var accountId = Guid.NewGuid();

            _dbMock
                .Setup(d => d.GetMitIdAccountBySubId(hashedSub))
                .ReturnsAsync((MitID_Account?)null);

            _accDbMock
                .Setup(a => a.GetAccountByIdAsync(accountId))
                .ReturnsAsync((Account?)null);

            // Act
            var result = await _sut.CreateFromClaimsAsync(user, accountId);

            // Assert
            Assert.Null(result);
            _dbMock.Verify(d => d.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
            _cacheMock.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_WhenNewAccount_CreatesAndCachesAndReturnsIsNewTrue()
        {
            // Arrange
            var sub = "new-mitid-user";
            var hashedSub = SubIdHasher.Hash(sub);
            var user = CreateUserWithClaims(sub, "1990-01-01");

            var accountId = new Guid("3657D51F-3BA1-46FD-9857-050666D85F9E");

            // Ingen eksisterende account på samme hashed SubId
            _dbMock
                .Setup(d => d.GetMitIdAccountBySubId(hashedSub))
                .ReturnsAsync((MitID_Account?)null);

            // Parent account MUST exist (nyt krav)
            _accDbMock
                .Setup(a => a.GetAccountByIdAsync(accountId))
                .ReturnsAsync(new Account(accountId, "test@example.com"));

            // Mapper DTO -> entity
            _mapperMock
                .Setup(m => m.Map<MitID_Account>(It.IsAny<MitIdAccountDto>()))
                .Returns((MitIdAccountDto src) => new MitID_Account
                {
                    ID = src.Id,
                    AccountID = src.AccountId,
                    SubID = src.SubId,
                    IsAdult = src.IsAdult
                });

            var newId = Guid.NewGuid();
            MitID_Account? capturedEntity = null;

            _dbMock
                .Setup(d => d.CreateMitIdAccount(It.IsAny<MitID_Account>()))
                .Callback<MitID_Account>(e => capturedEntity = e)
                .ReturnsAsync(newId);

            // Act
            var result = await _sut.CreateFromClaimsAsync(user, accountId);

            // Assert
            Assert.NotNull(result);
            Assert.True(result!.IsNew);
            Assert.NotNull(result.Account);

            var account = result.Account!;
            Assert.Equal(newId, account.Id);
            Assert.Equal(accountId, account.AccountId);
            Assert.Equal(hashedSub, account.SubId);
            Assert.True(account.IsAdult);

            // DB entity (det der bliver gemt) skal have korrekt AccountID/SubID
            Assert.NotNull(capturedEntity);
            Assert.Equal(accountId, capturedEntity!.AccountID);
            Assert.Equal(hashedSub, capturedEntity.SubID);

            _cacheMock.Verify(c => c.SetAsync(
                    It.Is<MitIdAccountDto>(d =>
                        d.Id == newId &&
                        d.AccountId == accountId &&
                        d.SubId == hashedSub),
                    It.IsAny<TimeSpan>()),
                Times.Once);
        }

        // ---------- GetByAccountIdAsync tests ----------

        [Fact]
        public async Task GetByAccountIdAsync_WhenInCache_ReturnsFromCacheAndSkipsDb()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var dto = new MitIdAccountDto
            {
                Id = Guid.NewGuid(),
                AccountId = accountId,
                SubId = "sub",
                IsAdult = true
            };

            _cacheMock
                .Setup(c => c.GetAsync(accountId))
                .ReturnsAsync(dto);

            // Act
            var result = await _sut.GetByAccountIdAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountId, result!.AccountId);

            _dbMock.Verify(d => d.GetMitIdAccountByAccId(It.IsAny<Guid>()), Times.Never);
        }

        [Fact]
        public async Task GetByAccountIdAsync_WhenNotInCacheAndNotInDb_ReturnsNull()
        {
            // Arrange
            var accountId = Guid.NewGuid();

            _cacheMock
                .Setup(c => c.GetAsync(accountId))
                .ReturnsAsync((MitIdAccountDto?)null);

            _dbMock
                .Setup(d => d.GetMitIdAccountByAccId(accountId))
                .ReturnsAsync((MitID_Account?)null);

            // Act
            var result = await _sut.GetByAccountIdAsync(accountId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByAccountIdAsync_WhenNotInCacheButInDb_ReturnsDtoAndCaches()
        {
            // Arrange
            var accountId = Guid.NewGuid();

            _cacheMock
                .Setup(c => c.GetAsync(accountId))
                .ReturnsAsync((MitIdAccountDto?)null);

            var entity = new MitID_Account
            {
                ID = Guid.NewGuid(),
                AccountID = accountId,
                SubID = "sub-from-db",
                IsAdult = true
            };

            _dbMock
                .Setup(d => d.GetMitIdAccountByAccId(accountId))
                .ReturnsAsync(entity);

            var dto = new MitIdAccountDto
            {
                Id = entity.ID,
                AccountId = entity.AccountID,
                SubId = entity.SubID,
                IsAdult = entity.IsAdult
            };

            _mapperMock
                .Setup(m => m.Map<MitIdAccountDto>(entity))
                .Returns(dto);

            // Act
            var result = await _sut.GetByAccountIdAsync(accountId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(accountId, result!.AccountId);

            _cacheMock.Verify(c => c.SetAsync(dto, It.IsAny<TimeSpan>()), Times.Once);
        }

        // ---------- GetAllAsync tests ----------

        [Fact]
        public async Task GetAllAsync_MapsEntitiesToDtos()
        {
            // Arrange
            var entities = new List<MitID_Account>
            {
                new MitID_Account
                {
                    ID = Guid.NewGuid(),
                    AccountID = Guid.NewGuid(),
                    SubID = "s1",
                    IsAdult = true
                },
                new MitID_Account
                {
                    ID = Guid.NewGuid(),
                    AccountID = Guid.NewGuid(),
                    SubID = "s2",
                    IsAdult = false
                }
            };

            _dbMock
                .Setup(d => d.GetAllMitIdAccounts())
                .ReturnsAsync(entities);

            _mapperMock
                .Setup(m => m.Map<List<MitIdAccountDto>>(entities))
                .Returns(new List<MitIdAccountDto>
                {
                    new MitIdAccountDto { Id = entities[0].ID, AccountId = entities[0].AccountID, SubId = entities[0].SubID },
                    new MitIdAccountDto { Id = entities[1].ID, AccountId = entities[1].AccountID, SubId = entities[1].SubID }
                });

            // Act
            var result = await _sut.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count);
        }

        // ---------- UpdateAsync tests ----------

        [Fact]
        public async Task UpdateAsync_WhenDbReturnsTrue_UpdatesCacheAndReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new MitIdAccountDto
            {
                Id = Guid.Empty,
                AccountId = Guid.NewGuid(),
                SubId = "sub",
                IsAdult = true
            };

            _mapperMock
                .Setup(m => m.Map<MitID_Account>(dto))
                .Returns(new MitID_Account
                {
                    ID = id,
                    AccountID = dto.AccountId,
                    SubID = dto.SubId,
                    IsAdult = dto.IsAdult
                });

            _dbMock
                .Setup(d => d.UpdateMitIdAccount(It.IsAny<MitID_Account>()))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.UpdateAsync(id, dto);

            // Assert
            Assert.True(result);

            _cacheMock.Verify(c => c.SetAsync(
                    It.Is<MitIdAccountDto>(d => d.Id == id),
                    It.IsAny<TimeSpan>()),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenDbReturnsFalse_DoesNotUpdateCacheAndReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();
            var dto = new MitIdAccountDto { Id = Guid.Empty };

            _mapperMock
                .Setup(m => m.Map<MitID_Account>(dto))
                .Returns(new MitID_Account { ID = id });

            _dbMock
                .Setup(d => d.UpdateMitIdAccount(It.IsAny<MitID_Account>()))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.UpdateAsync(id, dto);

            // Assert
            Assert.False(result);
            _cacheMock.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        // ---------- DeleteAsync tests ----------

        [Fact]
        public async Task DeleteAsync_WhenDbReturnsTrue_RemovesFromCacheAndReturnsTrue()
        {
            // Arrange
            var id = Guid.NewGuid();

            _dbMock
                .Setup(d => d.DeleteMitIdAccount(id))
                .ReturnsAsync(true);

            // Act
            var result = await _sut.DeleteAsync(id);

            // Assert
            Assert.True(result);
            _cacheMock.Verify(c => c.RemoveAsync(id), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenDbReturnsFalse_DoesNotTouchCacheAndReturnsFalse()
        {
            // Arrange
            var id = Guid.NewGuid();

            _dbMock
                .Setup(d => d.DeleteMitIdAccount(id))
                .ReturnsAsync(false);

            // Act
            var result = await _sut.DeleteAsync(id);

            // Assert
            Assert.False(result);
            _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<Guid>()), Times.Never);
        }
    }
}
