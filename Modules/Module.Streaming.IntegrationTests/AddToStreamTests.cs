using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Microsoft.Extensions.Configuration;
using Module.Streaming.Application;
using Module.Streaming.Infrastructure.Configuration;
using Xunit;

namespace Module.Streaming.IntegrationTests
{
    public class AddToStreamTests
    {
        [Fact]
        public async Task CorrectlyAddedToStream()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var aws = configuration.GetSection("aws")
                .Get<AwsOptions>();
            var awsEndpointPort = Environment.GetEnvironmentVariable("AwsEndpointPort");
            if (!string.IsNullOrEmpty(awsEndpointPort))
                aws.AwsEndpoint = $"http://localhost:{awsEndpointPort}";

            var firehoseConfig = new AmazonKinesisFirehoseConfig
            {
                ServiceURL = aws.AwsEndpoint
            };
            var fireHoseClient = new AmazonKinesisFirehoseClient(firehoseConfig);
            await fireHoseClient.CreateDeliveryStreamAsync(new CreateDeliveryStreamRequest
            {
                DeliveryStreamName = aws.FirehoseDeliveryStreamName
            });
            
            Startup.Initialize(aws.AwsEndpoint, aws.FirehoseDeliveryStreamName);

            var test = new
            {
                Test = "Hello World1"
            };
            
            await Streaming.Infrastructure.Configuration.Module.Execute(new AddToStream.Request
            {
                Payload = aws
            });

            await fireHoseClient.DeleteDeliveryStreamAsync(new DeleteDeliveryStreamRequest
            {
                DeliveryStreamName = aws.FirehoseDeliveryStreamName
            });
        }

        private record AwsOptions
        {
            public string AwsEndpoint { get; set; }
            public string FirehoseDeliveryStreamName { get; set; }
        }
    }
}