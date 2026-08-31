using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ServiceModel;

// DataServerInterface.cs - public WCF interface

namespace ServerInterfaceLib
{
    // Tells WCF that DataServerInterface defines operations that remote clients will be allowed to call
    [ServiceContract]
    public interface DataServerInterface
    {
        [OperationContract]
        int GetNumEntries();

        // Returns only the last name for one database record
        // Used by Business Tier when scannning records during a search
        [OperationContract]
        string GetLastNameForEntry(int index);

        [OperationContract]
        [FaultContract(typeof(string))] // WCF FaultContract - tells WCF that GetValuesForEntry() is allowed to return a fault containing a string message
        void GetValuesForEntry(
            int index, 
            out uint acctNo, 
            out uint pin, 
            out int bal, 
            out string fName, 
            out string lName,
            out byte[] profilePicture);
    }
}
