using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Universalis.Entities;
using Xunit;

namespace Universalis.DbAccess.Tests;

public class CharacterStoreTests : IClassFixture<DbFixture>
{
    private readonly DbFixture _fixture;

    public CharacterStoreTests(DbFixture fixture)
    {
        _fixture = fixture;
    }

#if DEBUG
    [Fact]
#endif
    public async Task Insert_Works()
    {
        var store = _fixture.Services.GetRequiredService<ICharacterStore>();
        var character = new Character("15084143697577", "John Doe", 23);

        await store.Insert(character);
    }

#if DEBUG
    [Fact]
#endif
    public async Task InsertRetrieve_Works()
    {
        var store = _fixture.Services.GetRequiredService<ICharacterStore>();
        var character = new Character("15084143697578", "John Doe", 23);

        await store.Insert(character);
        var result = await store.Retrieve("15084143697578");

        Assert.NotNull(result);
        Assert.Equal(character.ContentIdSha256, result.ContentIdSha256);
        Assert.Equal(character.Name, result.Name);
        Assert.Equal(character.WorldId, result.WorldId);
    }
    
#if DEBUG
    [Fact]
#endif
    public async Task InsertUpdateRetrieve_Works()
    {
        var store = _fixture.Services.GetRequiredService<ICharacterStore>();
        var character = new Character("15084143697579", "John Doe", 23);

        await store.Insert(character);
        var result = await store.Retrieve("15084143697579");

        Assert.NotNull(result);
        Assert.Equal(character.ContentIdSha256, result.ContentIdSha256);
        Assert.Equal(character.Name, result.Name);
        Assert.Equal(character.WorldId, result.WorldId);
    }

#if DEBUG
    [Fact]
#endif
    public async Task Retrieve_Missing_ReturnsNull()
    {
        var store = _fixture.Services.GetRequiredService<ICharacterStore>();
        var result = await store.Retrieve("15084143697580");

        Assert.Null(result);
    }
}