using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime.Dispatchers;

public class ListingsRemoveDispatcher : IConsumer<ListingsRemove>
{
    private readonly ISocketProcessor _sockets;
    private readonly ILogger<ListingsRemoveDispatcher> _logger;

    public ListingsRemoveDispatcher(ISocketProcessor sockets, ILogger<ListingsRemoveDispatcher> logger)
    {
        _sockets = sockets;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ListingsRemove> context)
    {
        _sockets.Publish(context.Message);
        _logger.LogInformation("Published message of type {EventType}", context.Message.Event);
        return Task.CompletedTask;
    }
}
