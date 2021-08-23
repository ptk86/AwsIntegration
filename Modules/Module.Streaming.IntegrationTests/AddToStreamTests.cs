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
using Shouldly;
using Xunit;

namespace Module.Streaming.IntegrationTests
{
    public class AddToStreamTests : IAsyncLifetime
    {
        private AmazonS3Client _s3Client;
        private string _bucketName;
        private string _roleName;
        private string _deliveryStreamName;

        [Fact]
        public async Task CorrectlyAddedToStream()
        {
            //arrange
            var test = new
            {
                Test = "Hello World1"
            };
            
            //act
            await Streaming.Infrastructure.Configuration.Module.Execute(new AddToStream.Request
            {
                Payload = test
            });

            //assert
            var listObjectsResponse = await _s3Client.ListObjectsAsync(new ListObjectsRequest
            {
                BucketName = _bucketName
            });
            var s3Object = listObjectsResponse.S3Objects.FirstOrDefault();
            var getObjectResponse = await _s3Client.GetObjectAsync(new GetObjectRequest
            {
                Key = s3Object.Key,
                BucketName = _bucketName
            });    
            StreamReader reader = new StreamReader( getObjectResponse.ResponseStream );
            string jsonFromTheBucket = await reader.ReadToEndAsync();
            
            listObjectsResponse.S3Objects.Count.ShouldBe(1);
            jsonFromTheBucket.ShouldBe("{\"Test\":\"Hello World1\"}");
        }

        private async Task SetupBucket(string awsEndpoint)
        {
            var fixture = new Fixture();
            _bucketName = fixture.Create<string>();
            
            _s3Client = new AmazonS3Client(new AmazonS3Config
            {
                ServiceURL = awsEndpoint,
                ForcePathStyle = true
            });
            
            await _s3Client.PutBucketAsync(new PutBucketRequest()
            {
                BucketName = _bucketName
            });
        }

        private async Task SetupFirehoseRole(string endpoint)
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

            _roleName = roleName;
            await identityManagementClient.CreateRoleAsync(new CreateRoleRequest
            {
                RoleName = _roleName
            });
            
            await identityManagementClient.PutRolePolicyAsync(new PutRolePolicyRequest
            {
                RoleName = _roleName,
                PolicyDocument = policyString
            });
        }

        private async Task SetupFirehose(string endpoint, string bucketName, string roleName)
        {
            var fixture = new Fixture();
            var deliveryStreamName =  fixture.Create<string>();
            
            var amazonKinesisFirehoseClient = new AmazonKinesisFirehoseClient(new AmazonKinesisFirehoseConfig
            {
                ServiceURL = endpoint
            });

            _deliveryStreamName = deliveryStreamName;
            await amazonKinesisFirehoseClient.CreateDeliveryStreamAsync(new CreateDeliveryStreamRequest
            {
                DeliveryStreamName = _deliveryStreamName,
                ExtendedS3DestinationConfiguration = new ExtendedS3DestinationConfiguration
                {
                    BucketARN = $"arn:aws:s3:::{bucketName}",
                    RoleARN = $"arn:aws:iam::000000000000:role/{roleName}"
                }
            });
        }

        public async Task InitializeAsync()
        {
            var awsEndpoint = $"http://localhost:{Environment.GetEnvironmentVariable("AwsEndpointPort")}";
            await SetupBucket(awsEndpoint);
            await SetupFirehoseRole(awsEndpoint);
            await SetupFirehose(awsEndpoint, _bucketName, _roleName);
            Startup.Initialize(awsEndpoint, _deliveryStreamName);
        }

        public Task DisposeAsync()
        {
            _s3Client.Dispose();
            _bucketName = null;
            _roleName = null;
            _deliveryStreamName = null;
            return Task.CompletedTask;
        }
    }
}