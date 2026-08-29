using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// DataStruct stores image bytes

namespace DatabaseLib
{
    internal class DataStruct
    {
        // Public fields to DataStruct
        public uint acctNo;
        public uint pin;
        public int balance;
        public string firstName;
        public string lastName;
        public byte[] profilePicture; // image field

        // Constructor
        public DataStruct()
        {
            acctNo = 0;
            pin = 0;
            balance = 0;
            firstName = "";
            lastName = "";
            profilePicture = null;
        }
    }
}
