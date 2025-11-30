using AutoMapper;
using BachMitID.Application.BusinessLogicLayer;
using BachMitID.Application.BusinessLogicLayer.Interface;
using BachMitID.Application.DTOs;
using BachMitID.Application.Security;
using BachMitID.Domain.Interfaces;
using BachMitID.Domain.Model;
using Moq;
using System.Security.Claims;
using Xunit;

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
        var mockCache = new Mock<IMitIdAccountCache>();

        MitID_Account? savedEntity = null;

        // repo gemmer entity og returnerer dens ID
        mockDb.Setup(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()))
              .Callback<MitID_Account>(e => savedEntity = e)
              .ReturnsAsync((MitID_Account e) => e.ID);

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
            mockMapper.Object,
            mockCache.Object);

        var user = CreateMitIdUser("sub-123", "1979-11-11");

        // act
        var result = await service.CreateFromClaimsAsync(user);

        // assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result!.Id); // Guid er genereret korrekt

        // *** SUB-ID HASH ***
        var expectedHash = SubIdHasher.Hash("sub-123");
        Assert.Equal(expectedHash, result.SubId);

        Assert.True(result.IsAdult);

        // entity der blev gemt i DB skal matche DTO
        Assert.NotNull(savedEntity);
        Assert.Equal(result.Id, savedEntity!.ID);
        Assert.Equal(expectedHash, savedEntity.SubID);

        // DB skal være kaldt præcis én gang
        mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Once);

        // Cache bør også være kaldt én gang med samme accountId
        mockCache.Verify(c => c.SetAsync(
                It.Is<MitIdAccountDto>(d => d.Id == result.Id),
                It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateFromClaimsAsync_MissingDob_ReturnsNull_AndDoesNotCallDb()
    {
        var mockDb = new Mock<IMitIdDbAccess>();
        var mockMapper = new Mock<IMapper>();
        var mockCache = new Mock<IMitIdAccountCache>();

        var service = new MitIdAccountService(
            mockDb.Object,
            mockMapper.Object,
            mockCache.Object);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "sub-123")
        };

        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "oidc"));

        var result = await service.CreateFromClaimsAsync(user);

        Assert.Null(result);
        mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task CreateFromClaimsAsync_NoSub_ReturnsNull_AndDoesNotHitDb()
    {
        var mockDb = new Mock<IMitIdDbAccess>();
        var mockMapper = new Mock<IMapper>();
        var mockCache = new Mock<IMitIdAccountCache>();

        var service = new MitIdAccountService(
            mockDb.Object,
            mockMapper.Object,
            mockCache.Object);

        var user = CreateUserWithClaims(new List<Claim>
        {
            new Claim(BD, "1979-11-11")
        });

        var result = await service.CreateFromClaimsAsync(user);

        Assert.Null(result);
        mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task CreateFromClaimsAsync_NoBirthdate_ReturnsNull_AndDoesNotHitDb()
    {
        var mockDb = new Mock<IMitIdDbAccess>();
        var mockMapper = new Mock<IMapper>();
        var mockCache = new Mock<IMitIdAccountCache>();

        var service = new MitIdAccountService(
            mockDb.Object,
            mockMapper.Object,
            mockCache.Object);

        var user = CreateUserWithClaims(new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "sub-123")
        });

        var result = await service.CreateFromClaimsAsync(user);

        Assert.Null(result);
        mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task CreateFromClaimsAsync_InvalidBirthdate_ReturnsNull()
    {
        var mockDb = new Mock<IMitIdDbAccess>();
        var mockMapper = new Mock<IMapper>();
        var mockCache = new Mock<IMitIdAccountCache>();

        var service = new MitIdAccountService(
            mockDb.Object,
            mockMapper.Object,
            mockCache.Object);

        var user = CreateUserWithClaims(new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "sub-123"),
            new Claim(BD, "not-a-date")
        });

        var result = await service.CreateFromClaimsAsync(user);

        Assert.Null(result);
        mockDb.Verify(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()), Times.Never);
        mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Fact]
    public async Task CreateFromClaimsAsync_DbThrows_ExceptionIsPropagated()
    {
        var mockDb = new Mock<IMitIdDbAccess>();
        var mockMapper = new Mock<IMapper>();
        var mockCache = new Mock<IMitIdAccountCache>();

        // mapper laver en entity med et Guid
        mockMapper.Setup(m => m.Map<MitID_Account>(It.IsAny<MitIdAccountDto>()))
                  .Returns(new MitID_Account
                  {
                      ID = Guid.NewGuid(),
                      AccountID = Guid.NewGuid(),
                      SubID = "test-sub",
                      IsAdult = true
                  });

        // DB fejler
        mockDb.Setup(db => db.CreateMitIdAccount(It.IsAny<MitID_Account>()))
              .ThrowsAsync(new Exception("DB is down"));

        var service = new MitIdAccountService(
            mockDb.Object,
            mockMapper.Object,
            mockCache.Object);

        var user = CreateUserWithClaims(new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "sub-123"),
            new Claim(BD, "1979-11-11")
        });

        await Assert.ThrowsAsync<Exception>(() => service.CreateFromClaimsAsync(user));

        // vi forventer stadig ikke cache-set når DB fejler
        mockCache.Verify(c => c.SetAsync(It.IsAny<MitIdAccountDto>(), It.IsAny<TimeSpan>()), Times.Never);
    }
}
