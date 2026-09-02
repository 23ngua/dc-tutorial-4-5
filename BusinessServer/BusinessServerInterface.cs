using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.CodeDom;

/*
 * BusinessServerInterface.cs - Mirrors the Data Tier interface, includes profile picture and fault contract
 *                            - A public WCF interface that the GUI will call.
 *                            - THIS IS THE BUSINESS TIER INTERFACE
 */

namespace BusinessServer
{
    [ServiceContract]
    public interface BusinessServerInterface
    {
        [OperationContract]
        int GetNumEntries();

        [OperationContract]
        [FaultContract(typeof(string))]
        void GetValuesForEntry(
            int index,
            out uint acctNo,
            out uint pin,
            out int bal,
            out string fName,
            out string lName,
            out byte[] profilePicture);

        // Searches for the first record with a matching last name.
        // Returns true if a match is found, otherwise false.
        [OperationContract]
        [FaultContract(typeof(string))]
        bool SearchByLastName(
            string lastName,
            out uint acctNo,
            out uint pin,
            out int bal,
            out string fName,
            out string lName,
            out byte[] profilePicture);
    }
}
