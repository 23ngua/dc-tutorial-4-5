using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

using DatabaseLib;  // connect DataServer to DatabaseLib
using ServerInterfaceLib;   // make ServerProg use interface from DLL

// DataServer.cs - class that implements WCF interface
// DataServer acts as the layer between the future remote client and DLL:
// Client -> DataServerInterface -> DataServer -> DatabaseClass -> List<DataStruct>

// ServerProg - sends images bytes through WCF

namespace ServerProg
{
    [ServiceBehavior(
        ConcurrencyMode = ConcurrencyMode.Multiple, // allows service to handle multiple calls concurrently
        UseSynchronizationContext = false)] // prevents WCF from relying on a synchronisation context for dispatching calls
    internal class DataServer : DataServerInterface
    {
        // Dataserver use a singleton:
        private readonly DatabaseClass database = DatabaseClass.Instance;
        
        // two methods so the class implements DataServerInterface
        public int GetNumEntries()  // calls DLL's GetNumRecords()
        {
            // when client calls GetNumEntries() the server will return the number of records in DatabaseClass
            return database.GetNumRecords(); // connects server method to DLL method
        }

        public void GetValuesForEntry(
            int index, 
            out uint acctNo, 
            out uint pin, 
            out int bal, 
            out string fName, 
            out string lName,
            out byte[] profilePicture)
        {
            // validate the index on the server
            if (index < 0 || index >= database.GetNumRecords())
            {
                // throw a WCF fault from server - matches [FaultContract(typeof(string))] declared in DataServerInterface.cs
                throw new FaultException<string>(   // creates WCF SOAP fault instead of normal .NET exception across the network
                    "The requested database index is outside the valid range.",
                    new FaultReason("Invalid database index")); // give WCF fault a reason
            }

            acctNo = database.GetAcctNoByIndex(index);
            pin = database.GetPINByIndex(index);
            bal = database.GetBalanceByIndex(index);
            fName = database.GetFirstNameByIndex(index);
            lName = database.GetLastNameByIndex(index);
            profilePicture = database.GetProfilePictureByIndex(index);
        }
    }
}
