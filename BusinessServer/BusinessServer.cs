using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;
using ServerInterfaceLib;
using System.IO;
using System.Runtime.CompilerServices;

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
        private static uint LogNumber = 0;  // Keeps track of number of Business Tier operations that have been logged (static so value is shared by all BusinessServer instances/clients)

        [MethodImpl(MethodImplOptions.Synchronized)] // Writes one synchronized entry to consol and BusinessLog.txt
        private static void Log(string logString)   // Static + Synchronized ensures only 1 BusinessTier thread can log at a time
        {
            LogNumber++; // Incrememnt inside the synchronized method so counter is thread-safe

            string line = $"{LogNumber}: {DateTime.Now:G} - {logString}";  // Include log number, date/time & detailed operation info

            Console.WriteLine(line); // Display log live in Business Tier console

            File.AppendAllText("BusinessLog.txt", line + Environment.NewLine);
        }

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
            int totalEntries = dataServer.GetNumEntries();  // Get number of records from DataTier first

            Log( // Log function call and returned value
                $"GetNumEntries() called. " +
                $"Arguments: none. " +
                $"Returned total entries: {totalEntries}.");

            return totalEntries;
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
            try
            {
                // Request the record from Data Tier
                dataServer.GetValuesForEntry(
                    index,
                    out acctNo,
                    out pin,
                    out bal,
                    out fName,
                    out lName,
                    out profilePicture);
            }
            catch (FaultException<string> ex)
            {
                // Log the fault recevied from Data Tier
                Log(
                    $"GetValuesForEntry() failed." +
                    $"Index: {index}. " +
                    $"Data Tier fault: {ex.Detail}");

                // Re-publish fault through Business Tier WCF boundary
                throw new FaultException<string>(ex.Detail, new FaultReason("DataTier fault"));
            }

            Log(  // Log input argument and returned record information
                $"GetValuesForEntry() called. " +
                $"Index: {index}. " +
                $"Returned Account No: {acctNo}, " +
                $"PIN: {pin:D4}, " +
                $"Balance: {bal:C}, " +
                $"First Name: {fName}, " +
                $"Last Name: {lName}, " +
                $"Profile Picture Size: {(profilePicture == null ? 0 : profilePicture.Length)} bytes.");
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

            lastName = lastName?.Trim(); // Clean up surname before validating and searching

            if (string.IsNullOrWhiteSpace(lastName)) // Reject blank name searches
            {
                Log("SearchByLastName() called with invalid input: blank surname.");

                throw new FaultException<string>("Please enter a last name.", new FaultReason("Invalid surname"));
            }

            bool validLastName = lastName.All(c => char.IsLetter(c) || c == ' ' || c == '-' || c == '\''); // Allow letters, spaces, hyphens and apostrophes

            if (!validLastName)
            {
                Log($"SearchByLastName() called with invalid input: \"{lastName}\".");

                throw new FaultException<string>("Last name can only contain letters, spaces, hyphens and apostrophes.", new FaultReason("Invalid surname"));
            }

            // Get the database size once before starting search - this avoids making another RPC call on every loop iteration
            int totalEntries = dataServer.GetNumEntries();

            for (int i =0; i < totalEntries; i++)
            {
                string currentLName = dataServer.GetLastNameForEntry(i);

                // Stop when first matching surname is found
                if (currentLName.Equals(lastName, StringComparison.OrdinalIgnoreCase))
                {
                    dataServer.GetValuesForEntry(   // Only fetch complete matching record once
                        i,
                        out acctNo,
                        out pin,
                        out bal,
                        out fName,
                        out lName,
                        out profilePicture);

                    Log( // Log successfull surname search and returned record
                        $"SearchByLastName() called. " +
                        $"Search argument: \"{lastName}\". " +
                        $"Match found at index: {i}. " +
                        $"Returned Account No: {acctNo}, " +
                        $"PIN: {pin:D4}, " +
                        $"Balance: {bal:C}, " +
                        $"First Name: {fName}, " +
                        $"Last Name: {lName}, " +
                        $"Profile Picture Size: {(profilePicture == null ? 0 : profilePicture.Length)} bytes.");

                    return true;
                }
            }

            // Log unsuccessful surname search
            Log(
                $"SearchByLastName() called. " +
                $"Search argument: \"{lastName}\". " +
                $"Result: no matching last name found after searching {totalEntries} records.");

            return false;   // No matching surname was found
        }
    }
}
