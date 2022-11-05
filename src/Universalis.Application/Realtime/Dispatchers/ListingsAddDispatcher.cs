using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime.Dispatchers;

public class ListingsAddDispatcher : IConsumer<ListingsAdd>
{
    private readonly ISocketProcessor _sockets;
    private readonly ILogger<ListingsAddDispatcher> _logger;

    public ListingsAddDispatcher(ISocketProcessor sockets, ILogger<ListingsAddDispatcher> logger)
    {
        _sockets = sockets;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<ListingsAdd> context)
    {
        _sockets.Publish(context.Message);
        _logger.LogInformation("Published message of type {EventType}", context.Message.Event);
        return Task.CompletedTask;
    }
}
