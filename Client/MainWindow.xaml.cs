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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.ServiceModel;
using System.Linq.Expressions;
using System.IO;
using System.Windows.Media.Imaging;
using BusinessServer;
using System.Runtime.Remoting.Messaging;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BusinessServerInterface foob;   // server interface field added

        public delegate bool SearchDelegate(    // Delegate used to run Business Tier surname search asynchronously
            string lastName,
            out uint acctNo,
            out uint pin,
            out int bal,
            out string fName,
            out string lName,
            out byte[] profilePicture);

        public MainWindow()
        {
            InitializeComponent();

            // WCF channel factory
            ChannelFactory<BusinessServerInterface> foobFactory;
            NetTcpBinding tcp = new NetTcpBinding();

            // ensures Client can receive profile-picture responses larger than WCF's default 64KB limit
            tcp.MaxReceivedMessageSize = 10 * 1024 * 1024;

            // Allow enough time for full 100,0000-record surname search
            tcp.SendTimeout = TimeSpan.FromMinutes(15);

            // setting server URL and creating the channel
            string URL = "net.tcp://localhost:8200/BusinessService";
            foobFactory = new ChannelFactory<BusinessServerInterface>(tcp, URL);
            foob = foobFactory.CreateChannel();

            ((IContextChannel)foob).OperationTimeout = TimeSpan.FromMinutes(15); // Explicit timeout for RPC operations on this channel

            // display the total number of records - actual RPC call to the server
            // client will contact server as soon as window starts and display the number of records in TotalNum
            TotalNum.Text = foob.GetNumEntries().ToString(); 
        }

        private void GoButton_Click(object sender, RoutedEventArgs e)   // Button logic
        {
            // local variables added
            int index = 0;
            string fName = "";
            string lName = "";
            int bal = 0;
            uint acct = 0;
            uint pin = 0;
            byte[] profilePicture = null;

            // read the index from the GUI
            if (!Int32.TryParse(IndexNum.Text, out index)) // Int32.Parse() throws an exception
            {
                MessageBox.Show("Please enter a valid numeric index."); // prevent non-numeric index input from crashing client
                return;
            }

            // index range validation
            if (index < 0 || index >= int.Parse(TotalNum.Text))
            {
                MessageBox.Show("Please enter an index between 0 and " + (int.Parse(TotalNum.Text) - 1) + ".");
                return;
            }

            try // catch WCF fault in the client - wrap remote call and GUI updates in try/catch
            {
                // call the server with GetValuesForEntry()
                foob.GetValuesForEntry( // actual remote call to WCF server
                    index,
                    out acct, 
                    out pin, 
                    out bal, 
                    out fName, 
                    out lName,
                    out profilePicture); // server fills each out variable with the selected database record

                // put returned values into GUI
                FNameBox.Text = fName;
                LNameBox.Text = lName;
                BalanceBox.Text = bal.ToString("C");
                AcctNoBox.Text = acct.ToString();
                PinBox.Text = pin.ToString("D4");

                if (profilePicture != null && profilePicture.Length > 0)
                {
                    using (MemoryStream stream = new MemoryStream(profilePicture))
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

        // Searches database by last name using Business Tier
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            // Get last name entered by user
            string lastName = SearchLastNameBox.Text.Trim();

            // Put GUI into waiting state while search runs
            SearchLastNameBox.IsReadOnly = true;
            IndexNum.IsReadOnly = true;

            // Make record fields read-only while search is running
            FNameBox.IsReadOnly = true;
            LNameBox.IsReadOnly = true;
            AcctNoBox.IsReadOnly = true;
            PinBox.IsReadOnly = true;
            BalanceBox.IsReadOnly = true;

            SearchButton.IsEnabled = false;
            GoButton.IsEnabled = false; // x:Name button is GoButton

            SearchProgressBar.IsIndeterminate = true;

            // Start asynchronous search.
            SearchDelegate searchDel = SearchBusinessTier;   // Run search using worker thread's own WCF connection
            AsyncCallback callbackDel = OnSearchCompletion;     // Callback will run when search finishes

            // Temporary out parameters required when starting delegate
            // The real completed values are retrieved later using EndInvoke()
            uint acctNo;
            uint pin;
            int bal;
            string fName;
            string lName;
            byte[] profilePicture;

            // Start search on a worker thread - returns immediately so WPF GUI remains responsive
            searchDel.BeginInvoke(
                lastName,
                out acctNo,
                out pin,
                out bal,
                out fName,
                out lName,
                out profilePicture,
                callbackDel,
                null);
        }

        // Performs surname RPC on worker thread
        // Separate WCF channel created here so the long-running search does not use Business Tier channel created by GUI thread
        private bool SearchBusinessTier(
            string lastName,
            out uint acctNo,
            out uint pin,
            out int bal,
            out string fName,
            out string lName,
            out byte[] profilePicture)
        {
            NetTcpBinding tcp = new NetTcpBinding();  // Creates a separate WCF binding for background search
            
            tcp.MaxReceivedMessageSize = 10 * 1024 * 1024;
            tcp.SendTimeout = TimeSpan.FromMinutes(15);

            string url = "net.tcp://localhost:8200/BusinessService";

            ChannelFactory<BusinessServerInterface> searchFactory = new ChannelFactory<BusinessServerInterface>(tcp, url);

            BusinessServerInterface searchChannel = searchFactory.CreateChannel();

            ((IContextChannel)searchChannel).OperationTimeout = TimeSpan.FromMinutes(15);   // Allow enough time for full 100,000 record search

            try
            {
                // Perform actual RPC on this worker-thread channel
                return searchChannel.SearchByLastName(
                    lastName,
                    out acctNo,
                    out pin,
                    out bal,
                    out fName,
                    out lName,
                    out profilePicture);
            }
            finally
            {
                // Clean up the temporary WCF connection
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

        // Runs on worker thread after asynchronous surname search finishes
        private void OnSearchCompletion(IAsyncResult asyncResult)
        {
            AsyncResult asyncObj = (AsyncResult)asyncResult;  // Get info about asynchronous delegate call

            SearchDelegate searchDel = (SearchDelegate)asyncObj.AsyncDelegate;  // Get delegate that originally started the search

            try
            {
                if (asyncObj.EndInvokeCalled == false)  // EndInvoke must only be called once
                {
                    bool found = searchDel.EndInvoke(   // Retrieve completed search result and out parameters
                        out uint acctNo,
                        out uint pin,
                        out int bal,
                        out string fName,
                        out string lName,
                        out byte[] profilePicture,
                        asyncResult);

                    Dispatcher.Invoke(new Action(() =>
                    {
                        if (found)
                        {
                            // Display matching record
                            FNameBox.Text = fName;
                            LNameBox.Text = lName;
                            AcctNoBox.Text = acctNo.ToString();
                            PinBox.Text = pin.ToString();
                            BalanceBox.Text = bal.ToString("C");

                            // Display returned profile picture
                            if (profilePicture != null && profilePicture.Length > 0)
                            {
                                using (MemoryStream stream = new MemoryStream(profilePicture))
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
                        else
                        {
                            // Search has finished = stop progress bar animation before display no-match message
                            SearchProgressBar.IsIndeterminate = false;

                            MessageBox.Show("No matching last name was found.", "Search Result", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }));
                }
            }
            catch (TimeoutException)
            {
                // Handle RPC timeout without crashing Client
                Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show("The search timed out before it could finish.", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }
            catch (FaultException<string> ex)
            {
                // Display the validation fault which is returned by the Business Tier
                Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show(ex.Detail, "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }
            catch (CommunicationException)
            {
                // Hnadle failed or lost WCF connection
                Dispatcher.Invoke(new Action(() =>
                {
                    MessageBox.Show("A communication error occurred while searching.", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }));
            }
            finally
            {
                Dispatcher.Invoke(new Action(() =>  // ALways restore GUI even if search fails
                {
                    SearchLastNameBox.IsReadOnly = false;
                    IndexNum.IsReadOnly = false;

                    FNameBox.IsReadOnly = false;
                    LNameBox.IsReadOnly = false;
                    AcctNoBox.IsReadOnly = false;
                    PinBox.IsReadOnly = false;
                    BalanceBox.IsReadOnly = false;

                    SearchButton.IsEnabled = true;
                    GoButton.IsEnabled = true;

                    // Stop progress bar
                    SearchProgressBar.IsIndeterminate = false;
                }));

                asyncObj.AsyncWaitHandle.Close();   // Clean up asynchronous operation
            }
        }
    }
}
