using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Reflection;

// DatabaseLib.cs - Tutorial 2: Class 1 Database Generator

namespace DatabaseLib
{
    
    // This class is a pseudo-random generator of database entries. Declare it as an internal class.
    internal class DatabaseGenerator
    {
        private List<byte[]> profilePictures;

        private Random rand;

        private string GetFirstname()
        {
            // Generate outputs randomly. Store first names in array and randomly choose one.
            string[] firstNames =
            {
                "James", "Emily", "Michael", "Sarah", "Daniel", "Olivia", "William", "Sophia", "Ethan", "Emma"
            };

            // Use rand.Next(firstNames.Length) to generate a random index
            // Returns the randomly selected first name
            return firstNames[rand.Next(firstNames.Length)];
        }

        private string GetLastname()    // Method to return a random last name
        {
            // Create array of sample last names. Choose one randomly.
            string[] lastNames =
            {
                "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis", "Wilson", "Taylor", "Anderson"
            };

            return lastNames[rand.Next(lastNames.Length)];  // Returns the selected surname
        }

        private uint GetPIN()   // Produce a 4-digit PIN
        {
            // Generates random number between 1000 and 9999
            // Casts result to uint because the method returns an unsigned integer.
            return (uint)rand.Next(1000, 10000);
        }

        private uint GetAcctNo()
        {
            // Generate an 8-digit account number
            return (uint)rand.Next(10000000, 99999999);
        }

        private int GetBalance()
        {
            // Generate a random account balance - can range from -5000 (overdrawn) up to 1000000.
            return rand.Next(-5000, 100001);
        }

        // Private profile-picture generator method
        private byte[] GetProfilePicture()
        {
            return profilePictures[rand.Next(profilePictures.Count)];
        }

        public DatabaseGenerator()
        {
            rand = new Random();
            
            profilePictures = new List<byte[]>();

            Assembly assembly = Assembly.GetExecutingAssembly();

            foreach (string resource in assembly.GetManifestResourceNames())
            {
                if (resource.EndsWith(".jpg") ||
                    resource.EndsWith(".jpeg") ||
                    resource.EndsWith(".png"))
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resource))
                    {
                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            stream.CopyTo(memoryStream);
                            profilePictures.Add(memoryStream.ToArray());
                        }
                    }
                }
            }
        }

        // Method calls each private generator method.
        // Assigns the results to the out parameters.
        public void GetNextAccount(
            out uint pin,
            out uint acctNo,
            out string firstName,
            out string lastName,
            out int balance,
            out byte[] profilePicture)
        {
            pin = GetPIN();
            acctNo = GetAcctNo();
            firstName = GetFirstname();
            lastName = GetLastname();
            balance = GetBalance();
            profilePicture = GetProfilePicture();
        }
    }
}
