using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

/*
 * Task 1. Building the business tier
 * - References ServerInterfaceLib.
 */

namespace BusinessServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Start the Business Tier server
            Console.WriteLine("Welcome to the Business Server!");

            // Create WCF service host
            ServiceHost host;

            // Use tcp for communication
            NetTcpBinding tcp = new NetTcpBinding();

            // Allow for larger messages (profile pictures)
            tcp.MaxReceivedMessageSize = 10 * 1024 * 1024;

            // Host the BusinessServer implementation
            host = new ServiceHost(typeof(BusinessServer));

            // ***Expose BusinessServerInterface on port 8200***
            host.AddServiceEndpoint(typeof(BusinessServerInterface), tcp, "net.tcp://0.0.0.0:8200/BusinessService"); // business tier endpoint

            // **Start Business Tier (keep server running)**
            host.Open();
            Console.WriteLine("Business Server Online");

            // Keep server running until Enter is pressed:
            Console.ReadLine();

            // Shut down
            host.Close();
        }
    }
}
