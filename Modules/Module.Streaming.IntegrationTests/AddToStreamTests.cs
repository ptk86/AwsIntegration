using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Amazon.S3;
using Amazon.S3.Model;
using AutoFixture;
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
            var awsEndpoint = $"http://localhost:{Environment.GetEnvironmentVariable("AwsEndpointPort")}";
            var bucketName = await SetupBucket(awsEndpoint);
            var roleName = await SetupFirehoseRole(awsEndpoint);
            var deliveryStreamName = await SetupFirehose(awsEndpoint, bucketName, roleName);
            Startup.Initialize(awsEndpoint, deliveryStreamName);
            
            var test = new
            {
                Test = "Hello World1"
            };
            
            await Streaming.Infrastructure.Configuration.Module.Execute(new AddToStream.Request
            {
                Payload = test
            });
        }

        private async Task<string> SetupBucket(string awsEndpoint)
        {
            var fixture = new Fixture();
            var bucketName = fixture.Create<string>();
            
            var s3Client = new AmazonS3Client(new AmazonS3Config
            {
                ServiceURL = awsEndpoint,
                ForcePathStyle = true
            });
            
            await s3Client.PutBucketAsync(new PutBucketRequest()
            {
                BucketName = bucketName
            });
            return bucketName;
        }

        private async Task<string> SetupFirehoseRole(string endpoint)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();
            var policyString = configuration.GetSection("firehoseRolePolicy")
                .Value;
            
            var fixture = new Fixture();
            var roleName = fixture.Create<string>();

            var identityManagementClient = new AmazonIdentityManagementServiceClient(new AmazonIdentityManagementServiceConfig
            {
                ServiceURL = endpoint
            });

            await identityManagementClient.CreateRoleAsync(new CreateRoleRequest
            {
                RoleName = roleName
            });
            
            await identityManagementClient.PutRolePolicyAsync(new PutRolePolicyRequest
            {
                RoleName = roleName,
                PolicyDocument = policyString
            });

            return roleName;
        }

        private async Task<string> SetupFirehose(string endpoint, string bucketName, string roleName)
        {
            var fixture = new Fixture();
            var deliveryStreamName =  fixture.Create<string>();
            
            var amazonKinesisFirehoseClient = new AmazonKinesisFirehoseClient(new AmazonKinesisFirehoseConfig
            {
                ServiceURL = endpoint
            });

            await amazonKinesisFirehoseClient.CreateDeliveryStreamAsync(new CreateDeliveryStreamRequest
            {
                DeliveryStreamName = deliveryStreamName,
                ExtendedS3DestinationConfiguration = new ExtendedS3DestinationConfiguration
                {
                    BucketARN = $"arn:aws:s3:::{bucketName}",
                    RoleARN = $"arn:aws:iam::000000000000:role/{roleName}"
                }
            });
            
            return deliveryStreamName;
        }
    }
}