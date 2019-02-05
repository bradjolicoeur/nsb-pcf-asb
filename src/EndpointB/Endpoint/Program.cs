using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus;
using NServiceBus.Logging;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EndpointB
{
    class Program
    {
        private static ILog log;

        public static IConfigurationRoot configuration;

        private static IEndpointInstance EndpointInstance { get; set; }

        static async Task Main()
        {
            // Create service collection
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            //Set console title
            Console.Title = configuration.GetSection("EndpointName").Value;

            //Configure logging
            LogManager.Use<DefaultFactory>()
                .Level(LogLevel.Info);
            log = LogManager.GetLogger<Program>();

            //Configure NSB Endpoint
            EndpointConfiguration endpointConfiguration = ConfigureNSB(serviceCollection);

            //Start NSB Endpoint
            EndpointInstance = await Endpoint.Start(endpointConfiguration)
                .ConfigureAwait(false);

            //Support Graceful Shut Down of NSB Endpoint in PCF
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Console.WriteLine("ENDPOINT READY");

            while (true)
            { 
                //just keep on trucking
            }

        }

        private static EndpointConfiguration ConfigureNSB(ServiceCollection serviceCollection)
        {
            string endpointName = configuration.GetSection("EndpointName").Value;

            var endpointConfiguration = new EndpointConfiguration(endpointName);
            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();

            var connectionString = GetConnectionString();
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new Exception("Could not read the 'AzureServiceBus_ConnectionString' environment variable. Check the sample prerequisites.");
            }
            transport.ConnectionString(connectionString);

            var conventions = endpointConfiguration.Conventions();
            ConfigureConventions(conventions);

            endpointConfiguration.EnableInstallers();
            endpointConfiguration.SendFailedMessagesTo("error");

            endpointConfiguration.UseContainer<ServicesBuilder>(
            customizations: customizations =>
            {
                customizations.ExistingServices(serviceCollection);
            });

            return endpointConfiguration;

        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            if (EndpointInstance != null)
            { EndpointInstance.Stop().ConfigureAwait(false); }

            log.Info("Exiting!");
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
            string connection = Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString");

            if (string.IsNullOrEmpty(connection))
                throw new Exception("Environment Variable 'AzureServiceBus_ConnectionString' not set");

            return connection;

        }
    }
}
