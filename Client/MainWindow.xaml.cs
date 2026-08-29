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

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BusinessServerInterface foob;   // server interface field added

        public MainWindow()
        {
            InitializeComponent();

            // WCF channel factory
            ChannelFactory<BusinessServerInterface> foobFactory;
            NetTcpBinding tcp = new NetTcpBinding();

            // ensures Client can receive profile-picture responses larger than WCF's default 64KB limit
            tcp.MaxReceivedMessageSize = 10 * 1024 * 1024; 

            // setting server URL and creating the channel
            string URL = "net.tcp://localhost:8200/BusinessService";
            foobFactory = new ChannelFactory<BusinessServerInterface>(tcp, URL);
            foob = foobFactory.CreateChannel();

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
    }
}
