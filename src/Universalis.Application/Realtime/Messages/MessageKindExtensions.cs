namespace Universalis.Application.Realtime.Messages;

public static class MessageKindExtensions
{
    public static string ToEventName(this MessageKind kind)
    {
        return kind switch
        {
            MessageKind.ItemUpdate => "update",
            MessageKind.Sales => "sales",
            MessageKind.Listings => "listings",
            _ => "unknown",
        };
    }
}