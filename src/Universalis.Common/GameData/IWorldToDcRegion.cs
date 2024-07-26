namespace Universalis.Common.GameData;

public interface IWorldToDcRegion
{
    (string Dc, string Region) Get(int worldId);
}
