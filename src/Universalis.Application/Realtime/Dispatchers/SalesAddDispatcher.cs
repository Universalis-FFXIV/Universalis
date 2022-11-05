using MassTransit;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime.Dispatchers;

public class SalesAddDispatcher : IConsumer<SalesAdd>
{
    private readonly ISocketProcessor _sockets;

    public SalesAddDispatcher(ISocketProcessor sockets)
    {
        _sockets = sockets;
    }

    public Task Consume(ConsumeContext<SalesAdd> context)
    {
        _sockets.Publish(context.Message);
        return Task.CompletedTask;
    }
}