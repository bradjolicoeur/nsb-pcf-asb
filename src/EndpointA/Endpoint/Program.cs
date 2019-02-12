using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Logging;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Example.NServiceBus.Messages;

namespace EndpointClient
{
    class Program
    {
        private static ILog log;

        public static IConfigurationRoot configuration;

        private static Guid EndpointId = Guid.NewGuid();

        private static IEndpointInstance EndpointInstance { get; set; }

        static async Task Main()
        {
            // Create service collection
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            //Set console title
            Console.Title = configuration.GetValue<string>("EndpointName");

            //Configure logging
            LogManager.Use<DefaultFactory>()
                .Level(LogLevel.Info);
            log = LogManager.GetLogger<Program>();

            //Configure NSB Endpoint
            EndpointConfiguration endpointConfiguration = ConfigureNSB(serviceCollection);

            //Start NSB Endpoint
            EndpointInstance = await Endpoint.Start(endpointConfiguration);

            //Support Graceful Shut Down of NSB Endpoint in PCF
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            log.Info("ENDPOINT READY");

            while (true)
            {

                var guid = Guid.NewGuid();
                log.Info($"Requesting to get data by id: {guid:N}");

                //create a message
                var message = new RequestDataMessage
                {
                    DataId = guid,
                    String = EndpointId.ToString()
                };

                //Send a message to a specific queue
                await EndpointInstance.Send("Samples.AzureServiceBus.EndpointB", message);

                // Sleep as long as you need.
                Thread.Sleep(1000);
            }

        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (EndpointInstance != null)
            { EndpointInstance.Stop().ConfigureAwait(false); }
            
            log.Info("Exiting!");
        }

        private static EndpointConfiguration ConfigureNSB(ServiceCollection serviceCollection)
        {

            var endpointConfiguration = new EndpointConfiguration(configuration.GetValue<string>("EndpointName"));

            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport.ConnectionString(GetConnectionString());

            var conventions = endpointConfiguration.Conventions();
            ConfigureConventions(conventions);

            endpointConfiguration.EnableInstallers(); //not for production

            endpointConfiguration.SendFailedMessagesTo("error");

            endpointConfiguration.UseContainer<ServicesBuilder>(
            customizations: customizations =>
            {
                customizations.ExistingServices(serviceCollection);
            });

            return endpointConfiguration;

        }

        private static void ConfigureServices(ServiceCollection serviceCollection)
        {
            configuration = new ConfigurationBuilder()
               .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
               .AddEnvironmentVariables()
               .AddCloudFoundry()
               .Build();

            // Add access to generic IConfigurationRoot
            serviceCollection.AddSingleton<IConfigurationRoot>(configuration);
        }

        private static string GetConnectionString()
        {
            string connection = configuration.GetValue<string>("AzureServiceBus_ConnectionString");

            if (string.IsNullOrEmpty(connection))
                throw new Exception("Environment Variable 'AzureServiceBus_ConnectionString' not set");

            return connection;

        }

        private static void ConfigureConventions(ConventionsBuilder conventions)
        {
            conventions.DefiningCommandsAs(
                type =>
                {
                    return type.Namespace == "Example.NServiceBus.Messages.Commands";
                });
            conventions.DefiningEventsAs(
                type =>
                {
                    return type.Namespace == "Example.NServiceBus.Messages.Events";
                });
            conventions.DefiningMessagesAs(
                type =>
                {
                    return type.Namespace == "Example.NServiceBus.Messages";
                });
            conventions.DefiningDataBusPropertiesAs(
                property =>
                {
                    return property.Name.EndsWith("DataBus");
                });
            conventions.DefiningExpressMessagesAs(
                type =>
                {
                    return type.Name.EndsWith("Express");
                });
            conventions.DefiningTimeToBeReceivedAs(
                type =>
                {
                    if (type.Name.EndsWith("Expires"))
                    {
                        return TimeSpan.FromSeconds(30);
                    }
                    return TimeSpan.MaxValue;
                });
        }
    }
}
