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
        // AutoResetEvent to signal when to exit the application.
        private static readonly AutoResetEvent waitHandle = new AutoResetEvent(false);

        public static IConfigurationRoot configuration;

        private static Guid EndpointId = Guid.NewGuid();

        static async Task Main()
        {
            // Create service collection
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            Console.Title = configuration.GetSection("EndpointName").Value;
            LogManager.Use<DefaultFactory>()
                .Level(LogLevel.Info);

            EndpointConfiguration endpointConfiguration = ConfigureNSB(serviceCollection);

            var endpointInstance = await Endpoint.Start(endpointConfiguration)
                .ConfigureAwait(false);

            Console.WriteLine("ENDPOINT READY");

            while (true)
            {

                var guid = Guid.NewGuid();
                Console.WriteLine($"Requesting to get data by id: {guid:N}");

                var message = new RequestDataMessage
                {
                    DataId = guid,
                    String = EndpointId.ToString()
                };
                await endpointInstance.Send("Samples.AzureServiceBus.EndpointB", message)
                    .ConfigureAwait(false);

                // Sleep as long as you need.
                Thread.Sleep(1000);
            }


        }

        private static EndpointConfiguration ConfigureNSB(ServiceCollection serviceCollection)
        {

            var endpointConfiguration = new EndpointConfiguration(configuration.GetSection("EndpointName").Value);

            var transport = endpointConfiguration.UseTransport<AzureServiceBusTransport>();
            transport.ConnectionString(GetConnectionString());

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
            string local = Environment.GetEnvironmentVariable("AzureServiceBus_ConnectionString");
            if (!string.IsNullOrEmpty(local))
                return local;

            //TODO: convert to use azure sb enviornment variable
            var credentials = "$..[?(@.name=='rabbitmq')].credentials";
            var jObj = JObject.Parse(Environment.GetEnvironmentVariable("VCAP_SERVICES"));

            if (jObj.SelectToken($"{credentials}") == null)
                throw new Exception("Expects a PCF managed rabbitmq service binding named 'rabbitmq'");

            var vhost = (string)jObj.SelectToken($"{credentials}.vhost");
            var host = (string)jObj.SelectToken($"{credentials}.hostname");
            var pwd = (string)jObj.SelectToken($"{credentials}.password");
            var username = (string)jObj.SelectToken($"{credentials}.username");

            string connectionString = $"host={host}; username={username}; password={pwd}; virtualhost={vhost}";

            Console.Out.WriteLine(connectionString);

            return connectionString;
        }
    }
}
