using MassTransit;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime.Dispatchers;

public class ListingsAddDispatcher : IConsumer<ListingsAdd>
{
    private readonly ISocketProcessor _sockets;

    public ListingsAddDispatcher(ISocketProcessor sockets)
    {
        _sockets = sockets;
    }

    public Task Consume(ConsumeContext<ListingsAdd> context)
    {
        _sockets.Publish(context.Message);
        return Task.CompletedTask;
    }
}
