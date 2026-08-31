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
        private DataServerInterface dataServer; // Remove connection to Data Tier

        public BusinessServer() // This constructor sets up connection from Business Tier to Data Tier (running on port 8100)
        {
            NetTcpBinding tcp = new NetTcpBinding();

            tcp.MaxReceivedMessageSize = 10 * 1024 * 1024;  // This allows for profile-picture messages larger than WCF's default limit***

            tcp.SendTimeout = TimeSpan.FromMinutes(5);  // Allow long-running Data Tier operations without timing out

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

        // Searches for the first record with a matching last name.
        // Returns true when a match is found, otherwise false.
        public bool SearchByLastName(
            string lastName,
            out uint acctNo,
            out uint pin,
            out int bal,
            out string fName,
            out string lName,
            out byte[] profilePicture)
        {
            // Set safe deault values in case no matching record is found.
            acctNo = 0;
            pin = 0;
            bal = 0;
            fName = "";
            lName = "";
            profilePicture = null;

            // Get the database size once before starting search - this avoids making another RPC call on every loop iteration
            int totalEntries = dataServer.GetNumEntries();

            for (int i =0; i < totalEntries; i++)
            {
                string currentLName = dataServer.GetLastNameForEntry(i);

                // Stop when first matching surname is found
                if (currentLName.Equals(lastName, StringComparison.OrdinalIgnoreCase))
                {
                    // Only now fetch complete matching record once
                    dataServer.GetValuesForEntry(
                        i,
                        out acctNo,
                        out pin,
                        out bal,
                        out fName,
                        out lName,
                        out profilePicture);

                    return true;
                }
            }

            // No matching surname was found
            return false;
        }
    }
}
