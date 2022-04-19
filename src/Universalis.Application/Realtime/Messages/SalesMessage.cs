namespace Universalis.Application.Realtime.Messages;

public class SalesMessage : SocketMessage
{
    public SalesMessage() : base(MessageKind.Sales)
    {
    }
}