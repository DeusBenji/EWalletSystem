using System;
using System.Text.Json;
using System.Threading.Tasks;
using BachMitID.Application.DTOs;
using BachMitID.Infrastructure.Cache;
using Moq;
using StackExchange.Redis;
using Xunit;
using System.Linq;
public class RedisTestMitIdAccount
{
    private MitIdAccountCache CreateCache(out Mock<IDatabase> dbMock)
    {
        var muxMock = new Mock<IConnectionMultiplexer>();
        dbMock = new Mock<IDatabase>();

        muxMock
            .Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(dbMock.Object);

        return new MitIdAccountCache(muxMock.Object);
    }

    [Fact]
    public async Task GetAsync_ReturnsNull_WhenRedisHasNoValue()
    {
        // arrange
        var cache = CreateCache(out var dbMock);
        var accountId = Guid.NewGuid();
        var key = $"mitid:account:{accountId}";

        dbMock
            .Setup(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString() == key),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // act
        var result = await cache.GetAsync(accountId);

        // assert
        Assert.Null(result);
        dbMock.Verify(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString() == key),
                It.IsAny<CommandFlags>()),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ReturnsDto_WhenValueExists()
    {
        // arrange
        var cache = CreateCache(out var dbMock);
        var accountId = Guid.NewGuid();
        var key = $"mitid:account:{accountId}";

        var dto = new MitIdAccountDto
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            SubId = "hashed-sub",
            IsAdult = true
        };

        var json = JsonSerializer.Serialize(dto);

        dbMock
            .Setup(db => db.StringGetAsync(
                It.Is<RedisKey>(k => k.ToString() == key),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisValue)json);

        // act
        var result = await cache.GetAsync(accountId);

        // assert
        Assert.NotNull(result);
        Assert.Equal(dto.Id, result!.Id);
        Assert.Equal(dto.AccountId, result.AccountId);
        Assert.Equal(dto.SubId, result.SubId);
        Assert.Equal(dto.IsAdult, result.IsAdult);
    }

   
    [Fact]
    public async Task SetAsync_CallsStringSetAsync_Once_WithExpectedArguments()
    {
        // arrange
        var cache = CreateCache(out var dbMock);
        var dto = new MitIdAccountDto
        {
            Id = Guid.NewGuid(),
            AccountId = Guid.NewGuid(),
            SubId = "hashed-sub",
            IsAdult = true
        };

        var ttl = TimeSpan.FromMinutes(30);
        var expectedKey = $"mitid:account:{dto.AccountId}";

        // act
        await cache.SetAsync(dto, ttl);

        // find alle kald til StringSetAsync
        var stringSetCalls = dbMock.Invocations
            .Where(i => i.Method.Name == "StringSetAsync")
            .ToList();

        // der skal være præcis ét kald
        Assert.Single(stringSetCalls);

        var call = stringSetCalls[0];

        // signature (effektivt): (key, value, expiry, when/flags/whatever)
        var keyArg = (RedisKey)call.Arguments[0];
        var valueArg = (RedisValue)call.Arguments[1];

        Assert.Equal(expectedKey, keyArg.ToString());

        // tjek at JSON indeholder vores dto
        var deserialized = JsonSerializer.Deserialize<MitIdAccountDto>(valueArg.ToString());
        Assert.NotNull(deserialized);
        Assert.Equal(dto.Id, deserialized!.Id);
        Assert.Equal(dto.AccountId, deserialized.AccountId);
        Assert.Equal(dto.SubId, deserialized.SubId);
        Assert.Equal(dto.IsAdult, deserialized.IsAdult);
    }


    [Fact]
        public async Task RemoveAsync_CallsKeyDelete_WithCorrectKey()
        {
            // arrange
            var cache = CreateCache(out var dbMock);
            var accountId = Guid.NewGuid();
            var expectedKey = $"mitid:account:{accountId}";

            dbMock
                .Setup(db => db.KeyDeleteAsync(
                    It.IsAny<RedisKey>(),
                    It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            // act
            await cache.RemoveAsync(accountId);

            // assert
            dbMock.Verify(db => db.KeyDeleteAsync(
                    It.Is<RedisKey>(k => k.ToString() == expectedKey),
                    It.IsAny<CommandFlags>()),
                Times.Once);
        }
}
