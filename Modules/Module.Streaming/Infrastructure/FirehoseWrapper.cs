using System.IO;
using System.Text;
using System.Threading.Tasks;
using Amazon.KinesisFirehose;
using Amazon.KinesisFirehose.Model;
using Newtonsoft.Json;

namespace Module.Streaming.Infrastructure
{
    public class FirehoseWrapper
    {       
        private readonly IAmazonKinesisFirehose _firehose;
        private readonly Config _config;

        public FirehoseWrapper(IAmazonKinesisFirehose firehose, Config config)
        {
            _firehose = firehose;
            _config = config;
        }
        public async Task PutMessage(object message)
        {
            var oByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));

            await using var ms = new MemoryStream(oByte);
            var requestRecord = new PutRecordRequest
            {
                DeliveryStreamName = _config.FirehoseDeliveryStreamName, Record = new Record {Data = ms},
            };
            var putRecordResponse = await _firehose.PutRecordAsync(requestRecord);
        }
        public class Config
        {
            public Config(string firehoseDeliveryStreamName)
            {
                FirehoseDeliveryStreamName = firehoseDeliveryStreamName;
            }
            public string FirehoseDeliveryStreamName { get; }
        }
    }
}