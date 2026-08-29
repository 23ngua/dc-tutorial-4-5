using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

using ServerInterfaceLib;

namespace ServerProg
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // ServiceHost and TCP binding
            Console.WriteLine("Welcome to the server");
            ServiceHost host;
            NetTcpBinding tcp = new NetTcpBinding();
            tcp.MaxReceivedMessageSize = 10 * 1024 * 1024; // max incoming WCF message size to 10MB

            // bind the host to DataServer
            host = new ServiceHost(typeof(DataServer)); // tells WCF that the service host should run internal DataServer

            // the WCF service endpoint - exposes public DataServerInterface over TCP on port 8100 (service name DataService)
            host.AddServiceEndpoint(
                typeof(DataServerInterface), tcp, "net.tcp://0.0.0.0:8100/DataService");

            // open the host and keep the server running
            host.Open(); // starts the WCF service
            Console.WriteLine("System Online"); // confirms the server has started
            Console.ReadLine(); // keeps the console open so the service stays running
            host.Close(); // shuts service down cleanly after pressing Enter
        }
    }
}
