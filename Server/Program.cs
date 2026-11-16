using System;
using System.ServiceModel;
using DataTierServer;
using SharedContracts;

namespace Server
{
    internal class Program
    {
        static void Main()
        {
            ServiceHost dataHost = null, lobbyHost = null;
            ChannelFactory<InterfaceData> dataFactory = null;
            try
            {
                //data
                var dataBinding = new NetTcpBinding { Security = { Mode = SecurityMode.None } };
                dataHost = new ServiceHost(typeof(DataService), new Uri("net.tcp://localhost:8000/DataService"));
                dataHost.AddServiceEndpoint(typeof(InterfaceData), dataBinding, "");
                dataHost.Open();

                //data proxy for lobby
                dataFactory = new ChannelFactory<InterfaceData>(dataBinding, new EndpointAddress("net.tcp://localhost:8000/DataService"));
                dataFactory.Open();
                var dataProxy = dataFactory.CreateChannel();

                //lobby service (separate duplex and polling contracts)
                var lobbyBinding = new NetTcpBinding
                {
                    Security = { Mode = SecurityMode.None },
                    ReceiveTimeout = TimeSpan.FromMinutes(10),
                    SendTimeout = TimeSpan.FromMinutes(2),
                    MaxReceivedMessageSize = 6 * 1024 * 1024,
                    MaxBufferSize = 6 * 1024 * 1024,
                    ReaderQuotas =
                    {
                        MaxArrayLength = 6 * 1024 * 1024,
                        MaxBytesPerRead = 64 * 1024,
                        MaxDepth = 64,
                        MaxNameTableCharCount = 64 * 1024,
                        MaxStringContentLength = 1024 * 1024
                    }
                };
                var lobbySvc = new LobbyService(dataProxy);
                lobbyHost = new ServiceHost(lobbySvc, new Uri("net.tcp://localhost:8001/LobbyService"));
                lobbyHost.AddServiceEndpoint(typeof(InterfaceLobbyDuplex), lobbyBinding, "Duplex");
                lobbyHost.AddServiceEndpoint(typeof(InterfaceLobbyPolling), lobbyBinding, "Polling");
                lobbyHost.Open();

                Console.WriteLine("Services running, ENTER to stop");
                Console.ReadLine();
            }
            finally
            {
                try { 
                    lobbyHost?.Close(); 
                } catch { 
                    lobbyHost?.Abort(); 
                }
                try { dataFactory?.Close(); } catch { dataFactory?.Abort(); }
                try { 
                    dataHost?.Close(); 
                } catch { 
                    dataHost?.Abort(); 
                }
            }
        }
    }
}
