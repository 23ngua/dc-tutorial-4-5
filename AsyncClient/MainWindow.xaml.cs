using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ServiceModel;
using BusinessServer;
using System.IO;
using System.Diagnostics;

namespace AsyncClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BusinessServerInterface foob;   // Connection to Business Tier

        private class SearchResult  // Stores result returned by asynchronous surname search
        {
            public bool Found { get; set; }

            public uint AcctNo { get; set; }
            public uint Pin { get; set; }
            public int Balance { get; set; }

            public string FirstName { get; set; }
            public string LastName { get; set; }

            public byte[] ProfilePicture { get; set; }
        }

        private SearchResult SearchBusinessTier(string lastName)    // Performs Business Tier surname search on background thread - returns 1 SearchResult object
        {
            NetTcpBinding tcp = new NetTcpBinding();

            tcp.MaxReceivedMessageSize = 10 * 1024 * 1024;

            tcp.SendTimeout = TimeSpan.FromMinutes(5); // Time out for business tier surname search

            string url = "net.tcp://localhost:8200/BusinessService";

            ChannelFactory<BusinessServerInterface> searchFactory = new ChannelFactory<BusinessServerInterface>(tcp, url);

            BusinessServerInterface searchChannel = searchFactory.CreateChannel();

            try
            {
                // Call the Business Tier search
                bool found = searchChannel.SearchByLastName(
                    lastName,
                    out uint acctNo,
                    out uint pin,
                    out int bal,
                    out string fName,
                    out string lName,
                    out byte[] profilePicture);

                // Package all returned values into 1 object
                return new SearchResult
                {
                    Found = found,
                    AcctNo = acctNo,
                    Pin = pin,
                    Balance = bal,
                    FirstName = fName,
                    LastName = lName,
                    ProfilePicture = profilePicture
                };
            }
            finally
            {
                // Clean up temp. WCF connection
                IClientChannel clientChannel = (IClientChannel)searchChannel;

                try
                {
                    if (clientChannel.State == CommunicationState.Faulted)
                    {
                        clientChannel.Abort();
                        searchFactory.Abort();
                    }
                    else
                    {
                        clientChannel.Close();
                        searchFactory.Close();
                    }
                }
                catch
                {
                    clientChannel.Abort();
                    searchFactory.Abort();
                }
            }
        }

        private void UpdateGui(SearchResult result) // Updates GUI with successful surname search result
        {
            FNameBox.Text = result.FirstName;
            LNameBox.Text = result.LastName;
            AcctNoBox.Text = result.AcctNo.ToString();
            PinBox.Text = result.Pin.ToString("D4");
            BalanceBox.Text = result.Balance.ToString("C");

            // Display returned profile picture
            if (result.ProfilePicture != null && result.ProfilePicture.Length > 0)
            {
                using (MemoryStream stream = new MemoryStream(result.ProfilePicture))
                {
                    BitmapImage bitmap = new BitmapImage();

                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = stream;
                    bitmap.EndInit();

                    ProfileImage.Source = bitmap;
                }
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            NetTcpBinding tcp = new NetTcpBinding();    // WCF connection to Business Tier
            
            tcp.MaxReceivedMessageSize = 10* 1024 * 1024;   // Allows pfp image size larger than WCF's default limit

            tcp.SendTimeout = TimeSpan.FromMinutes(15); // Enough time for 100,000 record surname search

            string URL = "net.tcp://localhost:8200/BusinessService";

            ChannelFactory<BusinessServerInterface> foobFactory = new ChannelFactory<BusinessServerInterface>(tcp, URL);

            foob = foobFactory.CreateChannel();

            ((IContextChannel)foob).OperationTimeout = TimeSpan.FromMinutes(15);    // Timeout for RPC operations on this channel

            TotalNum.Text = foob.GetNumEntries().ToString();    // Display total number of database records
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)   // Handles lookup by database index
        {
            // These variables will receive values from Business Tier:
            int index;
            string fName = "";
            string lName = "";
            int bal = 0;
            uint acctNo = 0;
            uint pin = 0;
            byte[] profilePicture = null;

            if(!Int32.TryParse(IndexNum.Text, out index))  // Validate that a user has entered a number
            {
                MessageBox.Show("Please enter a valid numeric index.");
                return;
            }

            // Check that index is within the valid database range
            if (index < 0 || index >= int.Parse(TotalNum.Text)) // Validate that the index is inside the database range
            {
                MessageBox.Show("Please enter an index between 0 and " + (int.Parse(TotalNum.Text) - 1) + ".");
                return;
            }

            try
            {
                // Request the selected record from Business Tier
                foob.GetValuesForEntry(
                    index,
                    out acctNo,
                    out pin,
                    out bal,
                    out fName,
                    out lName,
                    out profilePicture);

                // Display retured record
                FNameBox.Text = fName;
                LNameBox.Text = lName;
                AcctNoBox.Text = acctNo.ToString();
                PinBox.Text = pin.ToString("D4");
                BalanceBox.Text = bal.ToString("C");

                // Display returned profile pictureBi
                if (profilePicture != null && profilePicture.Length > 0)
                {
                    using(MemoryStream stream = new MemoryStream(profilePicture))
                    {
                        BitmapImage bitmap = new BitmapImage();

                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();

                        ProfileImage.Source = bitmap;
                    }
                }
            }
            catch (FaultException<string> ex)
            {
                MessageBox.Show(ex.Detail);
            }

        }

        private async void SearchButton_Click(Object sender, RoutedEventArgs e)   // Searches database by last name using async, await and Task
        {
            string lastName = SearchLastNameBox.Text.Trim();    // Get last name entered by user

            // This puts GUI into wiaitng state while the search runs:
            SearchLastNameBox.IsReadOnly = true;
            IndexNum.IsReadOnly = true;

            FNameBox.IsReadOnly = true;
            LNameBox.IsReadOnly= true;
            AcctNoBox.IsReadOnly = true;
            PinBox.IsReadOnly = true;
            BalanceBox.IsReadOnly = true;

            SearchButton.IsEnabled = false;
            GoButton.IsEnabled = false;

            SearchProgressBar.IsIndeterminate = true;

            try
            {
                SearchResult result = await Task.Run(() => SearchBusinessTier(lastName));    // Run long Business Tier search on worker thread. await returns control to GUI while task is running

                if (result.Found)   // After await, execution resumes on WPF GUI thread
                {
                    UpdateGui(result);
                }
                else
                {
                    SearchProgressBar.IsIndeterminate = false;  // Search complete - stop progress bar

                    MessageBox.Show("No matching last name was found.", "Search Result", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (FaultException<string> ex)
            {
                // Display informative Business Tier validation/fault message (Business Tier returns a controlled WCF fault)
                MessageBox.Show(ex.Detail, "Search Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (TimeoutException)
            {
                MessageBox.Show("The search timed out before it could finish.", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (CommunicationException)
            {
                MessageBox.Show("A communication error occurred while searching.", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Restore GUI whether search succeeds or fails
                SearchLastNameBox.IsReadOnly = false;
                IndexNum.IsReadOnly = false;

                FNameBox.IsReadOnly = false;
                LNameBox.IsReadOnly = false;
                AcctNoBox.IsReadOnly = false;
                PinBox.IsReadOnly = false;
                BalanceBox.IsReadOnly = false;

                SearchButton.IsEnabled = true;
                GoButton.IsEnabled = true;

                SearchProgressBar.IsIndeterminate = false;
            }
        }
    }
}
