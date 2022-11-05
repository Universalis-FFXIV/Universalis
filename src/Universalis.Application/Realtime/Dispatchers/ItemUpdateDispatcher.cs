using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime.Dispatchers;

public class ItemUpdateDispatcher : IConsumer<ItemUpdate>
{
    private readonly ISocketProcessor _sockets;
    private readonly ILogger<ItemUpdateDispatcher> _logger;

    public ItemUpdateDispatcher(ISocketProcessor sockets, ILogger<ItemUpdateDispatcher> logger)
    {
        _sockets = sockets;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ItemUpdate> context)
    {
        _sockets.Publish(context.Message);
        _logger.LogInformation("Published message of type {EventType}", context.Message.Event);
        return Task.CompletedTask;
    }
}
