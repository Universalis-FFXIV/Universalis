using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime.Dispatchers;

public class SalesAddDispatcher : IConsumer<SalesAdd>
{
    private readonly ISocketProcessor _sockets;
    private readonly ILogger<SalesAddDispatcher> _logger;

    public SalesAddDispatcher(ISocketProcessor sockets, ILogger<SalesAddDispatcher> logger)
    {
        _sockets = sockets;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<SalesAdd> context)
    {
        _sockets.Publish(context.Message);
        _logger.LogInformation("Published message of type {EventType}", context.Message.Event);
        return Task.CompletedTask;
    }
}