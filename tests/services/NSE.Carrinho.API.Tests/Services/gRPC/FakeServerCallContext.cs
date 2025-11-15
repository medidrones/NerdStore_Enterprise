using Grpc.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NSE.Carrinho.API.Tests.Services.gRPC
{
    public class FakeServerCallContext : ServerCallContext
    {
        protected override string MethodCore => "FakeMethod";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "peer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => new Metadata();
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore { get; } = new Metadata();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => null;

        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions options)
            => null;

        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders)
            => Task.CompletedTask;
    }
}
