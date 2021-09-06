using System;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Module.Streaming.Application;
using Module.Streaming.Infrastructure.Configuration;
using Newtonsoft.Json;
using Amazon.Lambda.S3Events;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace FirehoseLambda
{
    public class Function
    {
        public async Task FunctionHandler(S3Event input, ILambdaContext context)
        {
            var streamName = Environment.GetEnvironmentVariable("StreamName");
            var roleToAssume = Environment.GetEnvironmentVariable("RoleToAssume");
            Console.WriteLine($"Firehose stream to put to: {streamName}.");
            Console.WriteLine($"Input: {JsonConvert.SerializeObject(input.Records[0])}.");
            Console.WriteLine($"Role to assume: {roleToAssume}.");
            await Startup.Initialize(null, streamName, roleToAssume);
            await Module.Streaming.Infrastructure.Configuration.Module.Execute(new AddToStream.Request
            {
                Payload = input.Records[0]
            });
        }
    }

    public class Test
    {
        public object Event { get; set; }
    }
}
