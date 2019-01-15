using Example.NServiceBus.Messages;
using NServiceBus;
using NServiceBus.Logging;
using System.Threading.Tasks;

namespace EndpointClient
{
    public class DataResponseMessageHandler : IHandleMessages<DataResponseMessage>
    {
        static ILog log = LogManager.GetLogger<DataResponseMessageHandler>();

        public Task Handle(DataResponseMessage message, IMessageHandlerContext context)
        {
            log.Info($"Completed Message: {message.DataId}");
            return Task.CompletedTask;
        }
    }
}
