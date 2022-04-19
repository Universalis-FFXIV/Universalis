namespace Universalis.Application.Realtime.Messages;

public class ListingsMessage : SocketMessage
{
    public ListingsMessage() : base(MessageKind.Listings)
    {
    }
}