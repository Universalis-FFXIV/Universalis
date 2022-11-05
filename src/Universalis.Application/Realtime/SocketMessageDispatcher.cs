using MassTransit;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Universalis.Application.Realtime.Messages;

namespace Universalis.Application.Realtime
{
    public class SocketMessageDispatcher : IConsumer<SocketMessage>
    {
        private readonly ISocketProcessor _sockets;
        private readonly ILogger<SocketMessage> _logger;

        public SocketMessageDispatcher(ISocketProcessor sockets, ILogger<SocketMessage> logger)
        {
            _sockets = sockets;
            _logger = logger;
        }

        public Task Consume(ConsumeContext<SocketMessage> context)
        {
            _sockets.Publish(context.Message);
            _logger.LogInformation("Published message of type {EventType}", context.Message.Event);
            return Task.CompletedTask;
        }
    }
}
