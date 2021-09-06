using System.Threading.Tasks;
using Amazon;
using Amazon.Extensions.NETCore.Setup;
using Amazon.KinesisFirehose;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Module.Streaming.Infrastructure.Configuration
{
    public class Startup
    {
        public static async Task Initialize(string awsEndpoint, string deliveryStreamName, string roleToAssume = null)
        {
            var services = new ServiceCollection();
            services.AddMediatR(typeof(Startup).Assembly);

            var awsOptions = new AWSOptions();
            if (!string.IsNullOrEmpty(awsEndpoint))
            {
                awsOptions.DefaultClientConfig.ServiceURL = awsEndpoint;
            }
            
            if (!string.IsNullOrEmpty(roleToAssume))
            {
                var stsClient = new AmazonSecurityTokenServiceClient();
                var assumeRoleReq = new AssumeRoleRequest
                {
                    RoleArn = roleToAssume,
                    RoleSessionName = "session"
                };
                var response = await stsClient.AssumeRoleAsync(assumeRoleReq);
                awsOptions.Credentials = response.Credentials;
            }
            
            services.AddDefaultAWSOptions(awsOptions);
            services.AddAWSService<IAmazonKinesisFirehose>();
            services.AddSingleton<FirehoseWrapper>();
            services.AddSingleton(new FirehoseWrapper.Config(deliveryStreamName));
;
            CompositionRoot.SetContainer(services.BuildServiceProvider());
        }
    }

    internal static class CompositionRoot
    {
        private static ServiceProvider _container;

        public static void SetContainer(ServiceProvider container)
        {
            _container = container;
            _container.CreateScope();
        }

        public static IServiceScope BeginLifetimeScope()
        {
            return _container.CreateScope();
        }
    }

    public static class Module
    {
        public static async Task Execute(IRequest command)
        {
            using IServiceScope scope = CompositionRoot.BeginLifetimeScope();
            var mediator = scope.ServiceProvider.GetService<IMediator>();
            await mediator.Send(command);
        }
    }
}