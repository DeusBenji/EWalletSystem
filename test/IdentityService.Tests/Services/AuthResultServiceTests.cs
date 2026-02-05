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
    public class AuthResultServiceTests
    {
        private const string BD =
            "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/dateofbirth";

        private ClaimsPrincipal CreateMitIdUser(string sub, string birthdate)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, sub),
                new Claim(BD, birthdate),
                new Claim("name", "Bertil Von Testesen")
            };

            var identity = new ClaimsIdentity(claims, "oidc");
            return new ClaimsPrincipal(identity);
        }

        private ClaimsPrincipal CreateUserWithClaims(List<Claim> claims)
        {
            var identity = new ClaimsIdentity(claims, "oidc");
            return new ClaimsPrincipal(identity);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_ValidClaims_SavesToDb_AndReturnsDto()
        {
            // arrange
            var mockDb = new Mock<IMitIdDbAccess>();
            var mockMapper = new Mock<IMapper>();
            var mockAccDb = new Mock<IAccDbAccess>();
            var mockCache = new Mock<IMitIdAccountCache>();

            MitID_Account? savedEntity = null;

            var accountId = Guid.NewGuid();

            // Parent account MUST exist (nyt krav)
            mockAccDb.Setup(db => db.GetAccountByIdAsync(accountId))
                     .ReturnsAsync(new Account(accountId, "test@example.com"));

            // Ingen eksisterende account på samme hashed SubId
            mockDb.Setup(db => db.GetMitIdAccountBySubId(It.IsAny<string>()))
                  .ReturnsAsync((MitID_Account?)null);

            // repo gemmer entity og returnerer dens ID (det er den der bliver dto.Id bagefter)
            var returnedId = Guid.NewGuid();
            mockDb.Setup(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()))
                  .Callback<MitID_Account>(e => savedEntity = e)
                  .ReturnsAsync(returnedId);

            // mapper kopierer dto -> entity
            mockMapper.Setup(m => m.Map<MitID_Account>(It.IsAny<MitIdAccountDto>()))
                      .Returns((MitIdAccountDto dto) => new MitID_Account
                      {
                          ID = dto.Id,
                          AccountID = dto.AccountId,
                          SubID = dto.SubId,
                          IsAdult = dto.IsAdult
                      });

            var service = new MitIdAccountService(
                mockDb.Object,
                mockAccDb.Object,
                mockMapper.Object,
                mockCache.Object);

            var user = CreateMitIdUser("sub-123", "1979-11-11");

            // act
            var result = await service.CreateFromClaimsAsync(user, accountId);

            // assert
            Assert.NotNull(result);
            Assert.True(result!.IsNew); // skal være ny-oprettet i denne test

            var dto = result.Account;

            Assert.NotNull(dto);
            Assert.Equal(returnedId, dto.Id);              // id fra repo
            Assert.Equal(accountId, dto.AccountId);        // input accountId skal bæres med
            Assert.True(dto.IsAdult);

            // *** SUB-ID HASH ***
            var expectedHash = SubIdHasher.Hash("sub-123");
            Assert.Equal(expectedHash, dto.SubId);

            // entity der blev gemt i DB skal have korrekt AccountID/SubID
            Assert.NotNull(savedEntity);
            Assert.Equal(accountId, savedEntity!.AccountID);
            Assert.Equal(expectedHash, savedEntity.SubID);

            // DB skal være kaldt præcis én gang til Create
            mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Once);

            // Cache bør også være kaldt én gang
            mockCache.Verify(c => c.SetAsync(
                    It.Is<MitIdAccountDto>(d => d.Id == dto.Id),
                    It.IsAny<TimeSpan>()),
                Times.Once);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_MissingDob_ReturnsNull_AndDoesNotCallDb()
        {
            var mockDb = new Mock<IMitIdDbAccess>();
            var mockMapper = new Mock<IMapper>();
            var mockAccDb = new Mock<IAccDbAccess>();
            var mockCache = new Mock<IMitIdAccountCache>();

            var service = new MitIdAccountService(
                mockDb.Object,
                mockAccDb.Object,
                mockMapper.Object,
                mockCache.Object);

            var accountId = Guid.NewGuid();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "sub-123")
            };

            var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));

            var result = await service.CreateFromClaimsAsync(user, accountId);

            Assert.Null(result);
            mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
            mockAccDb.Verify(db => db.GetAccountByIdAsync(It.IsAny<Guid>()), Times.Never);
            mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_NoSub_ReturnsNull_AndDoesNotHitDb()
        {
            var mockDb = new Mock<IMitIdDbAccess>();
            var mockMapper = new Mock<IMapper>();
            var mockAccDb = new Mock<IAccDbAccess>();
            var mockCache = new Mock<IMitIdAccountCache>();

            var service = new MitIdAccountService(
                mockDb.Object,
                mockAccDb.Object,
                mockMapper.Object,
                mockCache.Object);

            var accountId = Guid.NewGuid();

            var user = CreateUserWithClaims(new List<Claim>
            {
                new Claim(BD, "1979-11-11")
            });

            var result = await service.CreateFromClaimsAsync(user, accountId);

            Assert.Null(result);
            mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
            mockAccDb.Verify(db => db.GetAccountByIdAsync(It.IsAny<Guid>()), Times.Never);
            mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_NoBirthdate_ReturnsNull_AndDoesNotHitDb()
        {
            var mockDb = new Mock<IMitIdDbAccess>();
            var mockMapper = new Mock<IMapper>();
            var mockAccDb = new Mock<IAccDbAccess>();
            var mockCache = new Mock<IMitIdAccountCache>();

            var service = new MitIdAccountService(
                mockDb.Object,
                mockAccDb.Object,
                mockMapper.Object,
                mockCache.Object);

            var accountId = Guid.NewGuid();

            var user = CreateUserWithClaims(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "sub-123")
            });

            var result = await service.CreateFromClaimsAsync(user, accountId);

            Assert.Null(result);
            mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
            mockAccDb.Verify(db => db.GetAccountByIdAsync(It.IsAny<Guid>()), Times.Never);
            mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_InvalidBirthdate_ReturnsNull()
        {
            var mockDb = new Mock<IMitIdDbAccess>();
            var mockMapper = new Mock<IMapper>();
            var mockAccDb = new Mock<IAccDbAccess>();
            var mockCache = new Mock<IMitIdAccountCache>();

            var service = new MitIdAccountService(
                mockDb.Object,
                mockAccDb.Object,
                mockMapper.Object,
                mockCache.Object);

            var accountId = Guid.NewGuid();

            var user = CreateUserWithClaims(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "sub-123"),
                new Claim(BD, "not-a-date")
            });

            var result = await service.CreateFromClaimsAsync(user, accountId);

            Assert.Null(result);
            mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
            mockAccDb.Verify(db => db.GetAccountByIdAsync(It.IsAny<Guid>()), Times.Never);
            mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
        }

        [Fact]
        public async Task CreateFromClaimsAsync_DbThrows_ExceptionIsPropagated()
        {
            var mockDb = new Mock<IMitIdDbAccess>();
            var mockMapper = new Mock<IMapper>();
            var mockAccDb = new Mock<IAccDbAccess>();
            var mockCache = new Mock<IMitIdAccountCache>();

            var accountId = Guid.NewGuid();

            // Parent account MUST exist (så vi kommer frem til DB create)
            mockAccDb.Setup(db => db.GetAccountByIdAsync(accountId))
                     .ReturnsAsync(new Account(accountId, "test@example.com"));

            // mapper laver en entity
            mockMapper.Setup(m => m.Map<MitID_Account>(It.IsAny<MitIdAccountDto>()))
                      .Returns((MitIdAccountDto dto) => new MitID_Account
                      {
                          ID = dto.Id,
                          AccountID = dto.AccountId,
                          SubID = dto.SubId,
                          IsAdult = dto.IsAdult
                      });

            // ingen eksisterende account på SubId
            mockDb.Setup(db => db.GetMitIdAccountBySubId(It.IsAny<string>()))
                  .ReturnsAsync((MitID_Account?)null);

            // DB fejler ved create
            mockDb.Setup(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()))
                  .ThrowsAsync(new Exception("DB is down"));

            var service = new MitIdAccountService(
                mockDb.Object,
                mockAccDb.Object,
                mockMapper.Object,
                mockCache.Object);

            var user = CreateUserWithClaims(new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "sub-123"),
                new Claim(BD, "1979-11-11")
            });

            await Assert.ThrowsAsync<Exception>(() => service.CreateFromClaimsAsync(user, accountId));

            // vi forventer stadig ikke cache-set når DB fejler
            mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
        }
    }
}
