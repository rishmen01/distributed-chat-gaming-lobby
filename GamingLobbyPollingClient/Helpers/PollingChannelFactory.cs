using System.ServiceModel;
using System.ServiceModel.Channels;

namespace GamingLobbyPollingClient.Helpers
{
    /// <summary>
    /// Names the non-duplex WCF channel factory used by the polling client.
    /// Polling itself is performed by MainWindow's background polling loop.
    /// </summary>
    public sealed class PollingChannelFactory<TChannel> : ChannelFactory<TChannel>
    {
        public PollingChannelFactory(Binding binding, EndpointAddress remoteAddress)
            : base(binding, remoteAddress)
        {
        }
    }
}
