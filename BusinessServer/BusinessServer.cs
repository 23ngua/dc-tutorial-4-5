using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;
using ServerInterfaceLib;

/*
 * BusinessServer.cs - This class sits between the Client GUI and Data Tier
 *                   - Implements the Business Tier WCF service
 *                   - This connects to the Data Tier using DataServerInterface
 */

namespace BusinessServer
{
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
    internal class BusinessServer : BusinessServerInterface
    {
        // Remove connection to Data Tier
        private DataServerInterface dataServer;

        // This constructor sets up connection from Business Tier to Data Tier (running on port 8100)
        public BusinessServer()
        {
            NetTcpBinding tcp = new NetTcpBinding();

            // This allows for profile-picture messages larger than WCF's default limit***
            tcp.MaxReceivedMessageSize = 10 * 2046 * 2046;

            string url = "net.tcp://localhost:8100/DataService";

            ChannelFactory<DataServerInterface> dataFactory = new ChannelFactory<DataServerInterface>(tcp, url);

            dataServer = dataFactory.CreateChannel();
        }
        
        // Return the total number of records from the Data Tier
        public int GetNumEntries()
        {
            return dataServer.GetNumEntries();
        }

        // Gets one record from Data Tier and passes it back to the Client
        public void GetValuesForEntry(
            int index,
            out uint acctNo,
            out uint pin,
            out int bal,
            out string fName,
            out string lName,
            out byte[] profilePicture)
        {
            dataServer.GetValuesForEntry(
                index,
                out acctNo,
                out pin,
                out bal,
                out fName,
                out lName,
                out profilePicture);
        }
    }
}
