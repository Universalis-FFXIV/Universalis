using System.Net.Http;

namespace Universalis.GameData;

public class RobustGameDataProviderParams
{
    public HttpClient Http { get; set; }

    public string SqPack { get; set; }
}