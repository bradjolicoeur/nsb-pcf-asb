using Example.NServiceBus.Messages;
using NServiceBus;
using NServiceBus.Logging;
using System;
using System.Threading.Tasks;

namespace EndpointClient
{
    public class DataResponseMessageHandler : IHandleMessages<DataResponseMessage>
    {

        public Task Handle(DataResponseMessage message, IMessageHandlerContext context)
        {
            Console.WriteLine($"Completed Message: {message.DataId} for endpoint {message.String}");
            return Task.CompletedTask;
        }
    }
}
