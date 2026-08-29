using DatabaseLib;
using System;
using System.Collections.Generic; 
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DatabaseLib
{
    // Publicly accessible object that will define the database
    public class DatabaseClass
    {
        // Making the data tier a singleton
        public static DatabaseClass Instance { get; } = new DatabaseClass();

        // Class field that holds a list of DataStruct objects
        List<DataStruct> dataStruct;

        // When DatabaseClass object created, constructor creates empty list to store DataStruct records
        private DatabaseClass()
        {
            dataStruct = new List<DataStruct>();

            // Create database records using DatabaseGenerator
            DatabaseGenerator generator = new DatabaseGenerator();

            // Creates 100,000 records for large number of entries
            for (int i = 0; i < 100000; i++)
            {
                DataStruct record = new DataStruct();

                generator.GetNextAccount(
                    out record.pin,
                    out record.acctNo,
                    out record.firstName,
                    out record.lastName,
                    out record.balance,
                    out record.profilePicture);

                dataStruct.Add(record);
            }
        }

        public uint GetAcctNoByIndex(int index)
        {
            return dataStruct[index].acctNo; // Returns acctNo stored at specific list index
        }

        public uint GetPINByIndex(int index)
        {
            return dataStruct[index].pin; // Returns PIN stored at the specified list index
        }

        public string GetFirstNameByIndex(int index)
        {
            return dataStruct[index].firstName; // Returns first name stored at specified index
        }

        public string GetLastNameByIndex(int index)
        {
            return dataStruct[index].lastName; // Returns last name stored at specific index
        }

        public int GetBalanceByIndex(int index)
        {
            return dataStruct[index].balance; // Returns balance stored at specific index
        }

        public int GetNumRecords()
        {
            return dataStruct.Count; // Returns total number of records currently stored in list
        }

        // profile picture getter in DatabaseClass
        public byte[] GetProfilePictureByIndex(int index)
        {
            return dataStruct[index].profilePicture;
        }
    }
}
