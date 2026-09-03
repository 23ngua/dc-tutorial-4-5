# dc-tutorial-4-5
COMP3008 Distributed Computing - Tutorial 4/5

Student Name: Amanda Huyen Nguyen
Student ID  : 22223850

My project is a multi-tier distributed C# application that is created using .NET Framework, WPF and WCF.

This application stores 100,000 generated account records and alows a user to:

- Search for an accoutn using its database index.
- Search for the first account matching a last name.
- View account number, PIN, balance, first name, last name and profile picture.
- Perform long-running surname searches without freezing the GUI.
- Handle invalid input, network faults and timeouts.
- Record Business Tier activity in an access log.


---

## System Architecture

My application uses the following architecture:

1. Client / AsyncClient
2. Business Tier
3. Data Tier
4. Database Lirbary

The Data Tier runs on port **8100**.

The Business Tier runs on port **8200**.

Both GUI applications communicate with the Business Tier rather than accessing the Data Tier directly.

---

## Projects

### DatabaseLib

This contains the application's generated database and accoutn records.

'DatabaseClass' uses the Singleton pattern so all Data Tier requests use the same database instance.

The database contains **100,000 generated records**.

---

### ServerInterfaceLib

This project contains 'DataServerInterface', which defines the WCF operations available between the Business Tier and Data Tier.

The interface uses:

- 'ServiceContract'
- 'OperationContract'
- 'FaultContract'


---

### ServerProg

This project implements the Data Tier.

It provides operations for:

- Getting the total number of records.
- Getting a last name for a particular index.
- Getting a complete account record.


The Data Tier validates database indexes and returns WCF faults when invalid requests are received.

---

### BusinessServer

This project implements the Business Tier.

The Business Tier connects to the Data Tier using:

'ChannelFactory<DataServerInterface>'

It provides the GUI clients with operations including:

- 'GetNumEntries()'
- 'GetValuesForEntry()'
- 'SearchByLastName()'

Surname searching is performed in the Business Tier rather than in the GUI.

The Business Tier also validates surname input and forwards controlled WCF faults back to the clients.

---

## Task 1 - Business Tier

A Business Tier was added between the GUI and Data Tier.

The GUI now connects to:

'net.tcp://localhost:8100/DataService'

The database was also changed to a Singleton so that every request uses the same generated database.

---

## Task 2 - Delegate Asynchronous Client

The original 'Client' performs surname searches asychronously using:

- Delegate
- 'BeginInvoke()'
- 'EndInvoke()'
- 'AsyncCallback'
- 'Dispatcher.Invoke()'

The surname search runs on a worker thread so the WPF GUI remains responsive.

Because the callback executes on a worker thread, 'Dispatcher.Invoke()' is sued when updating WPF controls.

During a search:

- The progress bar animates.
- Go! and Search are disabled.
- The window remains responsive.
- Controls are restored when the operation finished.

---

## Task 3 - Async/Await Client

A second WPF application called 'AsyncClient' performs the same functionality using:

- 'Task'
- 'Task.Run()'
- 'async'
- 'await'

The long WPF application called 'AsyncClient' performs the same functionality using:

'await Task.Run(...)'

After 'await', execution resumes on the WPF GUI thread, allowing the GUI to be updated directly without using 'Dispatcher.Invoke()'.

This demonstrates the difference between manual delegate-based asynchronous programming and the cleaner async/await approach.

---

## Task 4 - Business Tier Access Log

The Business Tier contains a synchronized logging system.

Each Business Tier operation records information including:

- Log number
- Date and time
- Function called
- Input arguments
- Returned information or error result

Logs are written to:

- The Business Server consol.
- 'BusinessLog.txt'

The logger uses:

'[MethodImpl(MethodImplOptions.Synchronized)]'

Both the log function and counter are static so all BusinessServer instances share the same coutner and synchronization lock.

This prevents duplicate log numbers and simultaneous file writes when multiple clients access the Business Tier at the same time.

---

## Task 5 - Exception Handling and Validation

Additional validation and exception handling were added throughout the system.

The application handles:

- Blank surname searches.
- Numbers entered as surnames.
- Special characters in surnames.
- Invalid database indexes.
- Missing surname matches.
- WCF communication errors.
- WCF faults.
- Search timeouts.

Surname inputs accepts letters, spaces, hyphens and apostrophes.

'FaultException<string>' and 'FaultContract' are used so errors can safely travel across the WCF network boundaries:

Data Tier -> Business Tier -> GUI

Timeouts are configured on the WCF bindings so a long-running network operation cannot leave the GUI permanently stuck in its waiting state.

---

## Running the Application

Start the projects in this order:

1. **ServerProg** - Data Tier
2. **BusinessServer** - Business Tier
3. **Client** or **AsyncClient** - GUI

The Data Tier console should display:

'System Online'

The Business Tier console should display:

'Business Server Online'

The GUI can then perform index and surname searches.

---

## Main Concepts Demonstrated

This assignment demonstrates:

- Multi-tier distributed application architecture
- WCF services and RPC
- Service and operation contracts
- ChannelFactory
- Signleton pattern
- Threading and asynchronous programming
- Delegates and callbacks
- WPF Dispatcher
- Async / Await / Task
- Thread synchronization
- Concurrent clients
- Business Tier logging
- Input validation
- Fault contracts
- Exception handling
- Network timeouts

