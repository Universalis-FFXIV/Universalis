using MassTransit;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime.Dispatchers;

public class ListingsRemoveDispatcher : IConsumer<ListingsRemove>
{
    private readonly ISocketProcessor _sockets;

    public ListingsRemoveDispatcher(ISocketProcessor sockets)
    {
        _sockets = sockets;
    }

    public Task Consume(ConsumeContext<ListingsRemove> context)
    {
        _sockets.Publish(context.Message);
        return Task.CompletedTask;
    }
}
