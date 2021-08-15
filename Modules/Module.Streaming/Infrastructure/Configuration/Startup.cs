using System.Threading.Tasks;
using Amazon.Extensions.NETCore.Setup;
using Amazon.KinesisFirehose;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Module.Streaming.Infrastructure.Configuration
{
    public class Startup
    {
        public static void Initialize(string awsEndpoint, string deliveryStreamName)
        {
            var services = new ServiceCollection();
            services.AddMediatR(typeof(Startup).Assembly);
            var awsOptions = new AWSOptions
            {
                DefaultClientConfig = {ServiceURL = awsEndpoint}
            };
            services.AddDefaultAWSOptions(awsOptions);
            services.AddAWSService<IAmazonKinesisFirehose>();
            services.AddTransient<FirehoseWrapper>();
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