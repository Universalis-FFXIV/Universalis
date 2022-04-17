namespace Universalis.Application.Realtime.Messages;

public static class MessageKindExtensions
{
    public static string ToEventName(this MessageKind kind)
    {
        return kind switch
        {
            MessageKind.ItemUpdate => "ITEM_UPDATE",
            _ => "UNKNOWN",
        };
    }
}