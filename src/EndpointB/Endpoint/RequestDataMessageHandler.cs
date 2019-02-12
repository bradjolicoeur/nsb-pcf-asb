using NServiceBus;
using System.Threading.Tasks;
using Example.NServiceBus.Messages;
using System;

namespace EndpointB
{

    public class RequestDataMessageHandler :
        IHandleMessages<RequestDataMessage>
    {
        public async Task Handle(RequestDataMessage message, IMessageHandlerContext context)
        {
            Console.WriteLine($"Received request {message.DataId}.");
            Console.WriteLine($"String received: {message.String}.");

            #region DataResponseReply

            var response = new DataResponseMessage
            {
                DataId = message.DataId,
                String = message.String
            };

            await context.Reply(response);

            #endregion
        }

    }

}