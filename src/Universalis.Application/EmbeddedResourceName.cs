namespace Universalis.Application;

public class EmbeddedResourceName
{
    private readonly string _resourceName;

    public EmbeddedResourceName(string resourceName)
    {
        _resourceName = resourceName;
    }

    public static implicit operator string(EmbeddedResourceName ern) =>
        nameof(Universalis) + "." + nameof(Application) + "." + ern._resourceName;
}