namespace Universalis.Application.Realtime.Message;

public static class MassageKindExtensions
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