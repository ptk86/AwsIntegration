using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Module.Streaming.Infrastructure;

namespace Module.Streaming.Application
{
    public class AddToStream
    {
        public class Request : IRequest
        {
            public object Payload { get; set; }
        }

        public class Handler : AsyncRequestHandler<Request>
        {
            private readonly FirehoseWrapper _firehoseWrapper;

            public Handler(FirehoseWrapper firehoseWrapper)
            {
                _firehoseWrapper = firehoseWrapper;
            }
            
            protected override async Task Handle(Request request, CancellationToken cancellationToken)
            {
                await _firehoseWrapper.PutMessage(request.Payload);
            }
        }
    }
}