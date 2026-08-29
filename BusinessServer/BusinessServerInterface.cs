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
    }
}
