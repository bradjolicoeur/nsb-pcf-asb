using NServiceBus;
using System.Threading.Tasks;
using NServiceBus.Logging;
using Microsoft.Extensions.Configuration;
using Example.NServiceBus.Messages;

public class RequestDataMessageHandler :
    IHandleMessages<RequestDataMessage>
{
    static ILog log = LogManager.GetLogger<RequestDataMessageHandler>();
    private string configValue { get; set; }

    public RequestDataMessageHandler(IConfigurationRoot configuration)
    {
        configValue = configuration.GetSection("toast").Value;
    }

    public async Task Handle(RequestDataMessage message, IMessageHandlerContext context)
    {
        log.Info($"Received request {message.DataId}.");
        log.Info($"String received: {message.String}.");

        #region DataResponseReply

        var response = new DataResponseMessage
        {
            DataId = message.DataId,
            String = configValue + " " + message.String
        };

        await context.Reply(response)
            .ConfigureAwait(false);

        #endregion
    }

}