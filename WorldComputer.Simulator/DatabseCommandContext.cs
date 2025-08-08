using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Text.Json;
using IdentityModel.Client;
using Microsoft.AspNetCore.Http;
using UnoSys.Api.Models;
using Unosys.SDK;
using FileStream = System.IO.FileStream;
using Path = System.IO.Path;
using System.Reflection.Metadata;
using Microsoft.VisualBasic;
using UnoSys.Api.Exceptions;

namespace WorldComputer.Simulator
{

    public class DatabaseCommandContext : ICommandContext
    {
        #region Field Members
        const int DEFAULT_CLIENT_NODE_NUMBER = 1;
        const int MAX_TITLE_LENGTH = 200;

        Hashtable switchSet = new Hashtable(20);
        string[] cmdArgs;
        string dbName = null!;
        bool listSwitch = false;
        string userName = null!;
        bool userSwitch = false;
        bool createSwitch = false;
        bool deleteSwitch = false;
        bool clientSimulator = false;
        bool simulatorHelp = false;
        bool openSwitch = false;
        bool closeSwitch = false;
        NetworkSpec NetworkSpec = null!;
        VDriveInfo VirtualDriveSpec = null!;
        int clientNodeNumber = DEFAULT_CLIENT_NODE_NUMBER;

        #endregion

        #region Constructors
        public DatabaseCommandContext(string[] commandContextArgs)
        {
            cmdArgs = commandContextArgs;
            // Define valid context switches
            string[] allowedSwitches = new string[8];
            allowedSwitches[0] = "/CLIENTNODE:";
            allowedSwitches[1] = "/CLOSE:";
            allowedSwitches[2] = "/CREATE";
            allowedSwitches[3] = "/DELETE";
            allowedSwitches[4] = "/LIST";
            allowedSwitches[5] = "/OPEN:";
            allowedSwitches[6] = "/USER:";
            allowedSwitches[7] = "/?";
            CommandLineTool.ParseCommandContextSwitches(commandContextArgs, allowedSwitches, switchSet, this);
            simulatorHelp = (bool)switchSet["/?"];
        }
        #endregion

        #region Public Methods
        public void ProcessCommand()
        {
            if (simulatorHelp)
            {
                CommandLineTool.DisplayBanner();
                DisplayUsage();
            }
            else
            {
                Program.UnoSysApiConnection = new UnoSysConnection(string.Format(Program.UnoSysApiUrlTemplate, Program.BasePort + ((clientNodeNumber - 1) * Program.BasePortSpacing)));

                #region Create DB
                if (createSwitch)
                {
                    CreateDatabase();
                }
                #endregion

                #region Delete DB
                if (deleteSwitch)
                {
                    DeleteDatabase();
                }
                #endregion

                #region Open DB
                if (openSwitch)
                {
                    OpenDatabase();
                }
                #endregion

                #region Close DB
                if (closeSwitch)
                {
                    CloseDatabase();
                }
                #endregion

                #region List
                if (listSwitch)
                {
                    ListTables();
                }
                #endregion

            }
        }

        public bool ValidateContext()
        {
            bool invalidSwitch = false;
            object switchValue = null;

            #region Switch Processing
            #region Create Database switch processing
            //Examples of possible legal switch usage: /Create:MyDatabase
            switchValue = switchSet["/CREATE"];
            if (switchValue is string)
            {
                if (!string.IsNullOrEmpty((string)switchValue))
                {
                    string[] values = ((string)switchValue).Split(new char[] { ',' }, StringSplitOptions.None); // NOTE: We do NOT throw away empty values in order to check for invalid switches
                    if (values.Length != 1)
                    {
                        invalidSwitch = true;
                    }
                    else
                    {
                        dbName = values[0];
                        createSwitch = true;
                    }
                    if (invalidSwitch)
                    {
                        throw new CommandLineToolInvalidSwitchException("/CREATE:" + switchValue);
                    }
                }
            }
            else
            {
                if ((bool)switchValue)
                {
                    throw new CommandLineToolInvalidSwitchException("/CREATE:" + switchValue);
                }
            }
            #endregion

            #region Delete Database switch processing
            //Examples of possible legal switch usage: /Delete:MyDatabase
            switchValue = switchSet["/DELETE"];
            if (switchValue is string)
            {
                if (!string.IsNullOrEmpty((string)switchValue))
                {
                    string[] values = ((string)switchValue).Split(new char[] { ',' }, StringSplitOptions.None); // NOTE: We do NOT throw away empty values in order to check for invalid switches
                    if (values.Length != 1)
                    {
                        invalidSwitch = true;
                    }
                    else
                    {
                        dbName = values[0];
                        deleteSwitch = true;
                    }
                    if (invalidSwitch)
                    {
                        throw new CommandLineToolInvalidSwitchException("/DELETE:" + switchValue);
                    }
                }
            }
            else
            {
                if ((bool)switchValue)
                {
                    throw new CommandLineToolInvalidSwitchException("/DELETE:" + switchValue);
                }
            }
            #endregion

            #region Open Database switch processing
            //Examples of possible legal switch usage: /Open:MyDatabase
            switchValue = switchSet["/OPEN"];
            if (switchValue is string)
            {
                if (!string.IsNullOrEmpty((string)switchValue))
                {
                    string[] values = ((string)switchValue).Split(new char[] { ',' }, StringSplitOptions.None); // NOTE: We do NOT throw away empty values in order to check for invalid switches
                    if (values.Length != 1)
                    {
                        invalidSwitch = true;
                    }
                    else
                    {
                        dbName = values[0];
                        openSwitch = true;
                    }
                    if (invalidSwitch)
                    {
                        throw new CommandLineToolInvalidSwitchException("/OPEN:" + switchValue);
                    }
                }
            }
            else
            {
                if ((bool)switchValue)
                {
                    throw new CommandLineToolInvalidSwitchException("/OPEN:" + switchValue);
                }
            }
            #endregion

            #region Close Database switch processing
            //Examples of possible legal switch usage: /Close:MyDatabase
            switchValue = switchSet["/CLOSE"];
            if (switchValue is string)
            {
                if (!string.IsNullOrEmpty((string)switchValue))
                {
                    string[] values = ((string)switchValue).Split(new char[] { ',' }, StringSplitOptions.None); // NOTE: We do NOT throw away empty values in order to check for invalid switches
                    if (values.Length != 1)
                    {
                        invalidSwitch = true;
                    }
                    else
                    {
                        dbName = values[0];
                        closeSwitch = true;
                    }
                    if (invalidSwitch)
                    {
                        throw new CommandLineToolInvalidSwitchException("/CLOSE:" + switchValue);
                    }
                }
            }
            else
            {
                if ((bool)switchValue)
                {
                    throw new CommandLineToolInvalidSwitchException("/CLOSE:" + switchValue);
                }
            }
            #endregion

            #region User switch processing
            switchValue = switchSet["/USER"];
            if (switchValue is string)
            {
                if (!string.IsNullOrEmpty((string)switchValue))
                {
                    string[] values = ((string)switchValue).Split(new char[] { ',' }, StringSplitOptions.None); // NOTE: We do NOT throw away empty values in order to check for invalid switches
                    if (values.Length != 1)
                    {
                        invalidSwitch = true;
                    }
                    else
                    {
                        userName = values[0];
                        userSwitch = true;
                    }
                    if (invalidSwitch)
                    {
                        throw new CommandLineToolInvalidSwitchException("/USER:" + switchValue);
                    }
                }
            }
            else
            {
                if ((bool)switchValue)
                {
                    throw new CommandLineToolInvalidSwitchException("/USER:" + switchValue);
                }
            }
            #endregion

            #region Client switch processing
            switchValue = switchSet["/CLIENTNODE"];
            if (switchValue is string)
            {
                if (!string.IsNullOrEmpty((string)switchValue))
                {
                    clientNodeNumber = int.Parse(((string)switchValue).ToUpper());
                }
                else
                {
                    clientNodeNumber = DEFAULT_CLIENT_NODE_NUMBER;
                }
            }
            else
            {
                if ((bool)switchValue)
                {
                    clientSimulator = true;
                    clientNodeNumber = DEFAULT_CLIENT_NODE_NUMBER;
                }
            }
            #endregion

            #region List Table switch processing
            //Examples of possible legal switch usage: /List
            switchValue = switchSet["/LIST"];
            if (switchValue is string)
            {
                if (!string.IsNullOrEmpty((string)switchValue))
                {
                    string[] values = ((string)switchValue).Split(new char[] { ',' }, StringSplitOptions.None); // NOTE: We do NOT throw away empty values in order to check for invalid switches
                    if (values.Length != 1)
                    {
                        invalidSwitch = true;
                    }
                    else
                    {
                        dbName = values[0];
                        listSwitch = true;
                    }
                    if (invalidSwitch)
                    {
                        throw new CommandLineToolInvalidSwitchException("/LIST:" + switchValue);
                    }
                }
            }
            else
            {
                if (!(switchValue is bool))
                {
                    listSwitch = false;
                }
                else
                {
                    listSwitch = (bool)switchValue;
                }
            }
            #endregion          
            #endregion


            #region Invalid Switch Combination Check
            if (createSwitch && deleteSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/CREATE and /DELETE cannot be used together.");
            }
            if (createSwitch && openSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/CREATE and /OPEN cannot be used together.");
            }
            if (createSwitch && closeSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/CREATE and /CLOSE cannot be used together.");
            }
            if (createSwitch && listSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/CREATE and /LIST cannot be used together.");
            }
            if (deleteSwitch && openSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/DELETE and /OPEN cannot be used together.");
            }
            if (deleteSwitch && closeSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/DELETE and /CLOSE cannot be used together.");
            }
            if (deleteSwitch && listSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/DELETE and /LIST cannot be used together.");
            }
            if (closeSwitch && openSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/CLOSE and /OPEN cannot be used together.");
            }
            if (closeSwitch && listSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/CLOSE and /LIST cannot be used together.");
            }
            if (openSwitch && listSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/OPEN and /LIST cannot be used together.");
            }
            if (!createSwitch && !simulatorHelp && !openSwitch && !deleteSwitch && !closeSwitch && !listSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("One of /CREATE or /OPEN or /DELETE or /CLOSE  or /LIST is required.");
            }
            #endregion

            bool result = (createSwitch || deleteSwitch || userSwitch || openSwitch || closeSwitch || clientSimulator || listSwitch || simulatorHelp);
            if (!result)
            {
                CommandLineTool.DisplayBanner();
                this.DisplayUsage();
                throw new CommandLineToolInvalidCommandException("At least one switch must be specified.");
            }
            return result;
        }

        public void DisplayUsage()
        {
            Program.DisplaySuccessfulWriteLine("World Computer DATABASE command usage:");
            Program.DisplaySuccessfulWriteLine("");
            Program.DisplaySuccessfulWriteLine("      WCSim DATABASE | DB  [<switches>]");
            Program.DisplaySuccessfulWriteLine("");
            Program.DisplaySuccessfulWriteLine("where <switches> are one or more of:");
            Program.DisplaySuccessfulWriteLine("");
            Program.DisplaySuccessfulWriteLine($" /CLIENTNODE:<Node#>\t\t\tClient Node # to connect to - ({DEFAULT_CLIENT_NODE_NUMBER})");
            Program.DisplaySuccessfulWriteLine(" /CLOSE:<DBName>\t\t\tDatabase Name to Close");
            Program.DisplaySuccessfulWriteLine(" /CREATE:<DBName>\t\t\tDatabase Name to Create");
            Program.DisplaySuccessfulWriteLine(" /DELETE:<DBName>\t\t\tDatabase Name to Delete");
            Program.DisplaySuccessfulWriteLine(" /LIST[:<DBName>]\t\t\tList available Tables associated with Database (currently open)");
            Program.DisplaySuccessfulWriteLine(" /OPEN:<DBName>\t\t\t\tDatabase Name to Open");
            Program.DisplaySuccessfulWriteLine(" /USER:<UserName>\t\t\tEstablish logged in User identity to execute Database operation under");
            Program.DisplaySuccessfulWriteLine(" /?\t\t\t\t\tUsage information");
            Program.DisplaySuccessfulWriteLine("");
            Program.DisplaySuccessfulWriteLine("=======================================================================================================");
        }
        #endregion


        #region Helpers
        private void ListTables()
        {
            UserSessionInfo userSessionInfo = null!;
            string[] tableNames = null;
            #region  Step #1:  Check if a simulation has already been created and if noto error
            if (!Simulator.NetworkAlreadyCreated())
            {
                throw new CommandLineToolInvalidOperationException($"A World Computer node network does not exist. Use 'WCSim NODE /CREATE' to create one first.");
            }
            #endregion

            #region Step #2: Deserialize the SIMULATOR_SETTINGS_FILE_NAME settings file
            using (System.IO.FileStream fs = new System.IO.FileStream(System.IO.Path.Combine(Program.WorkingDir, Simulator.SIMULATOR_SETTINGS_FILE_NAME), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                NetworkSpec = (NetworkSpec)JsonSerializer.Deserialize<NetworkSpec>(fs);
            }
            #endregion

            #region Step #3: Check if a node network is not already running and if so error
            if (!NetworkCommandContext.NetworkAlreadyRunning(NetworkSpec))
            {
                throw new CommandLineToolInvalidOperationException("A World Computer node network is not already running.  Use NODE /START to start one running one first.");
            }
            #endregion

            string result = "";
            #region Step #4:  Check if user already logged in
            var userLoggedIn = Simulator.UserAlreadyLoggedIn(clientNodeNumber, userName);
            if (!userLoggedIn)
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because User {userName.ToUpper()} is not logged in.");
                }
                else
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because no User context is established - use /USER switch, or login as a User.");
                }
            }
            else
            {

                if (userLoggedIn)
                {
                    #region Step #5: Deserialize UserSessionInfo to obtain the Token of the Logged in User session
                    userSessionInfo = Simulator.GetUserSessionInfo(clientNodeNumber, userName);
                    if (userSessionInfo == null) //|| !userName.Equals(userSessionInfo.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new CommandLineToolInvalidOperationException($"Corrupted WCSim user file.");
                    }
                    userName = userSessionInfo.UserName;
                    #endregion
                }

                #region Step #6:  Check if a Database is already opened by the User
                if (userSessionInfo.DBConnections == null || userSessionInfo.DBConnections.Count == 0)
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because no Database is opened by User {userName} - use DATABASE /Open:<DatabaseName> command to open one.");
                }
                if (userSessionInfo.DBConnections.Count > 1)
                {
                    if (dbName == null)
                    {
                        throw new CommandLineToolInvalidOperationException($"Cannot perform operation because User {userName} has more than one Database opened - use /DB:<name> switch to specify Database.");
                    }
                    else
                    {
                        if (!userSessionInfo.DBConnections.ContainsKey(dbName.ToUpper()))
                        {
                            throw new CommandLineToolInvalidOperationException($"Cannot perform operation because User {userName} does not have Database {dbName} opened - use DATABASE /Open:<DatabaseName> command to open it.");
                        }
                    }
                }
                if (dbName == null || dbName.Length == 0)
                {
                    dbName = userSessionInfo.DBConnections.First().Key.ToUpper(); // If no dbName specified, use the first one opened by the User
                }
                if (!userSessionInfo.DBConnections.ContainsKey(dbName.ToUpper()))
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because User {userName} does not have Database {dbName} opened - use DATABASE /Open:<DatabaseName> command to open it.");
                }
                #endregion


                #region  Step #7:  Call the WorldComputer CallGetTableNamesApiOnNode() Api to obtain an array of Tables names associated with the User's dbName Database
                tableNames = CallDatabaseReadTableNamesApiOnNode(userSessionInfo, dbName.ToUpper());
                #endregion
            }
            #endregion

            #region Step #7 - Display result
            Program.DisplaySuccessfulWriteLine($"");
            if (tableNames != null && tableNames.Length > 0)
            {
                Array.Sort(tableNames); // Sort the results alphabetically    
                foreach (var dbName in tableNames)
                {
                    result += $"{dbName}\n";
                }
                Program.DisplaySuccessfulWriteLine(result);
            }
            else
            {
                Program.DisplaySuccessfulWriteLine($"No tables found in Database {dbName} for User {userName}.");
            }
            #endregion
        }


        private void CreateDatabase()
        {
            bool result = false;
            string msg = null!;
            UserSessionInfo userSessionInfo = null!;
            #region  Step #1:  Check if a simulation has already been created and if noto error
            if (!Simulator.NetworkAlreadyCreated())
            {
                throw new CommandLineToolInvalidOperationException($"A World Computer node network does not exist. Use 'WCSim NODE /CREATE' to create one first.");
            }
            #endregion

            #region Step #2: Deserialize the SIMULATOR_SETTINGS_FILE_NAME settings file
            using (FileStream fs = new FileStream(Path.Combine(Program.WorkingDir, Simulator.SIMULATOR_SETTINGS_FILE_NAME), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                NetworkSpec = (NetworkSpec)JsonSerializer.Deserialize<NetworkSpec>(fs);
            }
            #endregion

            #region Step #3: Check if a node network is not already running and if so error
            if (!NetworkCommandContext.NetworkAlreadyRunning(NetworkSpec))
            {
                throw new CommandLineToolInvalidOperationException("A World Computer node network is not already running.  Use NODE /START to start one running one first.");
            }
            #endregion

            #region  Step #4:  Check if a vdrive has already been created and if so error
            if (!Simulator.VirtualDriveAlreadyCreated())
            {
                throw new CommandLineToolInvalidOperationException($"A World Computer Virtual Drive does not exist. Use 'VDRIVE /Create' to create one first.");
            }
            #endregion

            #region Step #5: Deserialize the SIMULATOR_VDRIVE_FILE_NAME settings file
            VirtualDriveSpec = Simulator.GetVirtualDriveSpec();
            #endregion

            #region Step #6:  Check if user already logged in
            var userLoggedIn = Simulator.UserAlreadyLoggedIn(clientNodeNumber, userName);
            if (!userLoggedIn)
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because User {userName.ToUpper()} is not logged in.");
                }
                else
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because no User context is established - use /USER switch, or login as a User.");
                }
            }
            else
            {

                if (userLoggedIn)
                {
                    #region Step #7: Deserialize UserSessionInfo to obtain the Token of the Logged in User session
                    userSessionInfo = Simulator.GetUserSessionInfo(clientNodeNumber, userName);
                    if (userSessionInfo == null) //|| !userName.Equals(userSessionInfo.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new CommandLineToolInvalidOperationException($"Corrupted WCSim user file.");
                    }
                    userName = userSessionInfo.UserName;
                    #endregion
                }


                #region  Step #8:  Call the WorldComputer CreateDatabase() Api to create a database associated with the User
                //result = CallCreateDatabaseApiOnNode(userSessionInfo.UserSessionToken, dbName);
                msg = $"Databases {dbName} Created for User {userName}.";
                API.Initialize(new Guid(VirtualDriveSpec.VirtualDiskSessionToken.Substring(1)),
                                new Guid(VirtualDriveSpec.VolumeSessionToken.Substring(1)),
                                userSessionInfo.UserSessionToken);
                try
                {
                    API.CreateDatabaseAsync(dbName).Wait();
                    result = true;
                }
                catch (AggregateException aex)
                {
                    if (aex.InnerExceptions[0] is UnoSysConflictException)
                    {
                        msg = "Database already exists.";
                    }
                    else
                    {
                        msg = aex.InnerExceptions[0].Message;
                    }
                    result = false;
                }

                catch (Exception ex)
                {
                    msg = ex.Message;
                    result = false;
                }
                

                //result += CallGetDatabaseNamesApiOnNode(userSessionInfo.UserSessionToken);

                #endregion
            }
            #endregion

            #region Step #9 - Display result
            Program.DisplaySuccessfulWriteLine($"");
            if (result)
            {
                Program.DisplaySuccessfulWriteLine($"Databases {dbName} Created for User {userName}.");
            }
            else
            {
                Program.DisplayUnsuccessfulWriteLine($"Databases {dbName} failed to be Created for User {userName} - {msg}.");
            }
            #endregion
        }


        private void DeleteDatabase()
        {
            bool result = false;
            string msg = null!;
            UserSessionInfo userSessionInfo = null!;
            #region  Step #1:  Check if a simulation has already been created and if noto error
            if (!Simulator.NetworkAlreadyCreated())
            {
                throw new CommandLineToolInvalidOperationException($"A World Computer node network does not exist. Use 'WCSim NODE /CREATE' to create one first.");
            }
            #endregion

            #region Step #2: Deserialize the SIMULATOR_SETTINGS_FILE_NAME settings file
            using (FileStream fs = new FileStream(Path.Combine(Program.WorkingDir, Simulator.SIMULATOR_SETTINGS_FILE_NAME), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                NetworkSpec = (NetworkSpec)JsonSerializer.Deserialize<NetworkSpec>(fs);
            }
            #endregion


            #region Step #3: Check if a node network is not already running and if so error
            if (!NetworkCommandContext.NetworkAlreadyRunning(NetworkSpec))
            {
                throw new CommandLineToolInvalidOperationException("A World Computer node network is not already running.  Use NODE /START to start one running one first.");
            }
            #endregion


            #region  Step #4:  Check if a vdrive has already been created and if so error
            if (!Simulator.VirtualDriveAlreadyCreated())
            {
                throw new CommandLineToolInvalidOperationException($"A World Computer Virtual Drive does not exist. Use 'VDRIVE /Create' to create one first.");
            }
            #endregion

            #region Step #5: Deserialize the SIMULATOR_VDRIVE_FILE_NAME settings file
            VirtualDriveSpec = Simulator.GetVirtualDriveSpec();
            #endregion

           
            #region Step #6:  Check if user already logged in
            var userLoggedIn = Simulator.UserAlreadyLoggedIn(clientNodeNumber, userName);
            if (!userLoggedIn)
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because User {userName.ToUpper()} is not logged in.");
                }
                else
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because no User context is established - use /USER switch, or login as a User.");
                }
            }
            else
            {

                if (userLoggedIn)
                {
                    #region Step #5: Deserialize UserSessionInfo to obtain the Token of the Logged in User session
                    userSessionInfo = Simulator.GetUserSessionInfo(clientNodeNumber, userName);
                    if (userSessionInfo == null) //|| !userName.Equals(userSessionInfo.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new CommandLineToolInvalidOperationException($"Corrupted WCSim user file.");
                    }
                    userName = userSessionInfo.UserName;
                    #endregion
                }


                //#region  Step #6:  Call the WorldComputer DeleteDatabase() Api to create a database associated with the User
                //result = CallDeleteDatabaseApiOnNode(userSessionInfo.UserSessionToken, dbName);

                ////result += CallGetDatabaseNamesApiOnNode(userSessionInfo.UserSessionToken);

                //#endregion
                #region  Step #7:  Call the WorldComputer CreateDatabase() Api to create a database associated with the User
                //result = CallCreateDatabaseApiOnNode(userSessionInfo.UseSessionToken, dbName);
                API.Initialize(new Guid(VirtualDriveSpec.VirtualDiskSessionToken.Substring(1)),
                                new Guid(VirtualDriveSpec.VolumeSessionToken.Substring(1)),
                                userSessionInfo.UserSessionToken);
                try
                {
                    API.DeleteDatabaseAsync(dbName).Wait();
                    result = true;
                }
                catch (AggregateException aex)
                {
                    if (aex.InnerExceptions[0] is UnoSysConflictException)
                    {
                        msg = "Database not found.";
                    }
                    else
                    {
                        msg = aex.InnerExceptions[0].Message;
                    }
                    result = false;
                }
                catch (Exception ex)
                {
                    msg = ex.Message;
                    result = false;
                }
                
                //result += CallGetDatabaseNamesApiOnNode(userSessionInfo.UserSessionToken);

                #endregion
            }
            #endregion

            #region Step #8 - Display result
            Program.DisplaySuccessfulWriteLine($"");
            if (result)
            { 
                Program.DisplaySuccessfulWriteLine($"Databases {dbName} Deleted for User {userName}.");
            }
            else
            {
                Program.DisplayUnsuccessfulWriteLine($"Databases {dbName} could not be Deleted for User {userName} - {msg}.");
            }
            #endregion
        }


        private void OpenDatabase()
        {
            string result = null!;
            UserSessionInfo userSessionInfo = null!;
            #region  Step #1:  Check if a simulation has already been created and if noto error
            if (!Simulator.NetworkAlreadyCreated())
            {
                throw new CommandLineToolInvalidOperationException($"A World Computer node network does not exist. Use 'WCSim NODE /CREATE' to create one first.");
            }
            #endregion

            #region Step #2: Deserialize the SIMULATOR_SETTINGS_FILE_NAME settings file
            using (FileStream fs = new FileStream(Path.Combine(Program.WorkingDir, Simulator.SIMULATOR_SETTINGS_FILE_NAME), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                NetworkSpec = (NetworkSpec)JsonSerializer.Deserialize<NetworkSpec>(fs);
            }
            #endregion

            #region Step #3: Check if a node network is not already running and if so error
            if (!NetworkCommandContext.NetworkAlreadyRunning(NetworkSpec))
            {
                throw new CommandLineToolInvalidOperationException("A World Computer node network is not already running.  Use NODE /START to start one running one first.");
            }
            #endregion

            #region Step #4:  Check if user already logged in
            var userLoggedIn = Simulator.UserAlreadyLoggedIn(clientNodeNumber, userName);
            if (!userLoggedIn)
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because User {userName.ToUpper()} is not logged in.");
                }
                else
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because no User context is established - use /USER switch, or login as a User.");
                }
            }
            else
            {

                if (userLoggedIn)
                {
                    #region Step #5: Deserialize UserSessionInfo to obtain the Token of the Logged in User session
                    userSessionInfo = Simulator.GetUserSessionInfo(clientNodeNumber, userName);
                    if (userSessionInfo == null) //|| !userName.Equals(userSessionInfo.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new CommandLineToolInvalidOperationException($"Corrupted WCSim user file.");
                    }
                    userName = userSessionInfo.UserName;
                    #endregion
                }

                #region Step #6:  Check if the database has already been connected to by thhis User
                if(userSessionInfo.DBConnections.ContainsKey(dbName.ToUpper()))
                {
                    throw new CommandLineToolInvalidOperationException($"User {userName} is already connected to database {dbName}.");
                }
                #endregion 

                #region  Step #7:  Call the WorldComputer DeleteDatabase() Api to create a database associated with the User
                result = CallConnectToDatabaseApiOnNode(userSessionInfo.UserSessionToken, dbName, 3, 2);
                if( result != null )
                {
                    userSessionInfo.DBConnections.Add(dbName.ToUpper(), result);
                    Simulator.UpdateUserSessionInfo(clientNodeNumber, userName, userSessionInfo);
                }

                //result += CallGetDatabaseNamesApiOnNode(userSessionInfo.UserSessionToken);

                #endregion
            }
            #endregion

            #region Step #7 - Display result
            Program.DisplaySuccessfulWriteLine($"");
            if (result != null)
            {
                Program.DisplaySuccessfulWriteLine($"User {userName} has connected to Databases {dbName}.");
            }
            else
            {
                Program.DisplayUnsuccessfulWriteLine($"User {userName} failed to connect to database {dbName}.");
            }
            #endregion
        }


        private void CloseDatabase()
        {
            bool result = false;
            UserSessionInfo userSessionInfo = null!;
            #region  Step #1:  Check if a simulation has already been created and if noto error
            if (!Simulator.NetworkAlreadyCreated())
            {
                throw new CommandLineToolInvalidOperationException($"A World Computer node network does not exist. Use 'WCSim NODE /CREATE' to create one first.");
            }
            #endregion

            #region Step #2: Deserialize the SIMULATOR_SETTINGS_FILE_NAME settings file
            using (FileStream fs = new FileStream(Path.Combine(Program.WorkingDir, Simulator.SIMULATOR_SETTINGS_FILE_NAME), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            {
                NetworkSpec = (NetworkSpec)JsonSerializer.Deserialize<NetworkSpec>(fs);
            }
            #endregion

            #region Step #3: Check if a node network is not already running and if so error
            if (!NetworkCommandContext.NetworkAlreadyRunning(NetworkSpec))
            {
                throw new CommandLineToolInvalidOperationException("A World Computer node network is not already running.  Use NODE /START to start one running one first.");
            }
            #endregion

            #region Step #4:  Check if user already logged in
            var userLoggedIn = Simulator.UserAlreadyLoggedIn(clientNodeNumber, userName);
            if (!userLoggedIn)
            {
                if (!string.IsNullOrEmpty(userName))
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because User {userName.ToUpper()} is not logged in.");
                }
                else
                {
                    throw new CommandLineToolInvalidOperationException($"Cannot perform operation because no User context is established - use /USER switch, or login as a User.");
                }
            }
            else
            {

                if (userLoggedIn)
                {
                    #region Step #5: Deserialize UserSessionInfo to obtain the Token of the Logged in User session
                    userSessionInfo = Simulator.GetUserSessionInfo(clientNodeNumber, userName);
                    if (userSessionInfo == null) //|| !userName.Equals(userSessionInfo.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new CommandLineToolInvalidOperationException($"Corrupted WCSim user file.");
                    }
                    userName = userSessionInfo.UserName;
                    #endregion
                }

                #region Step #6:  Check if the database has already been connected to by thhis User
                if (!userSessionInfo.DBConnections.ContainsKey(dbName.ToUpper()))
                {
                    throw new CommandLineToolInvalidOperationException($"User {userName} is not connected to database {dbName}.");
                }
                #endregion 

                #region  Step #7:  Call the WorldComputer DeleteDatabase() Api to create a database associated with the User
                result = CallDisconnectFromDatabaseApiOnNode(userSessionInfo.UserSessionToken, userSessionInfo.DBConnections[dbName.ToUpper()]);
                if (result)
                { 
                    userSessionInfo.DBConnections.Remove(dbName.ToUpper());
                    Simulator.UpdateUserSessionInfo(clientNodeNumber, userName, userSessionInfo);
                }

                #endregion
            }
            #endregion

            #region Step #7 - Display result
            Program.DisplaySuccessfulWriteLine($"");
            if (result)
            {
                Program.DisplaySuccessfulWriteLine($"User {userName} has disconnected from Databases {dbName}.");
            }
            else
            {
                Program.DisplayUnsuccessfulWriteLine($"User {userName} failed to disconnect from database {dbName}.");
            }   
            #endregion
        }

        private static string ConvertBytesToHexString(byte[] buffer)
        {
            byte[] bytes = new byte[buffer.Length];
            Buffer.BlockCopy(buffer, 0, bytes, 0, buffer.Length);
            string sbytes = BitConverter.ToString(bytes);       // Convert to hyphen delimited string of hex characters
            return sbytes.Replace("-", "");
        }

        private static byte[] ConvertHexStringToBytes(string hexString)
        {
            // Convert Hex string to byte[]
            byte[] HexAsBytes = new byte[hexString.Length / 2];
            for (int index = 0; index < HexAsBytes.Length; index++)
            {
                string byteValue = hexString.Substring(index * 2, 2);
                HexAsBytes[index] = byte.Parse(byteValue, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
            }
            return HexAsBytes;
        }


        static public class Console2
        {
            /// <summary>
            /// Like System.Console.ReadLine(), only with a mask.
            /// </summary>
            /// <param name="mask">a <c>char</c> representing your choice of console mask</param>
            /// <returns>the string the user typed in </returns>
            public static string ReadPassword(char mask)
            {
                const int ENTER = 13, BACKSP = 8, CTRLBACKSP = 127;
                int[] FILTERED = { 0, 27, 9, 10 /*, 32 space, if you care */ }; // const

                var pass = new Stack<char>();
                char chr = (char)0;

                while ((chr = System.Console.ReadKey(true).KeyChar) != ENTER)
                {
                    if (chr == BACKSP)
                    {
                        if (pass.Count > 0)
                        {
                            System.Console.Write("\b \b");
                            pass.Pop();
                        }
                    }
                    else if (chr == CTRLBACKSP)
                    {
                        while (pass.Count > 0)
                        {
                            System.Console.Write("\b \b");
                            pass.Pop();
                        }
                    }
                    else if (FILTERED.Count(x => chr == x) > 0) { }
                    else
                    {
                        pass.Push((char)chr);
                        System.Console.Write(mask);
                    }
                }

                System.Console.WriteLine();

                return new string(pass.Reverse().ToArray());
            }

            /// <summary>
            /// Like System.Console.ReadLine(), only with a mask.
            /// </summary>
            /// <returns>the string the user typed in </returns>
            public static string ReadPassword()
            {
                return Console2.ReadPassword('*');
            }
        }



        private string[] CallDatabaseReadTableNamesApiOnNode(UserSessionInfo userSessionInfo, string dbName)
        {
            string[] result = null!;
            var json = @$"
            {{
                ""V"" : 1,
                ""F"" : 0,
                ""A"" : {/*(int)ApiIdentifier.DatabaseReadTableNames*/302},
                ""I"" : [{{
                            ""D"" : {{
                                        ""N"" : ""UserSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{userSessionInfo.UserSessionToken}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""DatabaseSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{userSessionInfo.DBConnections[dbName]}"",
                            ""N""  : false
                        }}]
            }}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var nodeName = $"{Program.NodeExecutableName} #{clientNodeNumber}";
            try
            {

                var response = Program.UnoSysApiConnection.PostAsync(content).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // Retrieve result of call
                    var responseJson = response.Content.ReadAsStringAsync().Result;
                    using var doc = JsonDocument.Parse(responseJson);
                    var element = doc.RootElement;
                    result = JsonSerializer.Deserialize<string[]>(element.GetProperty("O")[0].GetProperty("V").GetString()!);
                    //result = element.GetProperty("O")[0].GetProperty("V").GetString()!;
                    //var responseResult = element.GetProperty("O")[0].TryGetStringArray("V");
                }
                else
                {
                    Program.DisplaySuccessfulWriteLine($"Error calling CallDatabaseReadTableNamesApiOnNode() - response.StatusCode={response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Program.DisplayUnsuccessfulWriteLine($"Error in CallDatabaseReadTableNamesApiOnNode(): {nodeName} - {ex} ");
            }
            return result;

        }


        private bool CallCreateDatabaseApiOnNode(string userSessionToken, string dbName)
        {
            bool result = false;
            var json = @$"
            {{
                ""V"" : 1,
                ""F"" : 0,
                ""A"" : {/*(int)ApiIdentifier.UserCreateDatabase*/504},
                ""I"" : [{{
                            ""D"" : {{
                                        ""N"" : ""UserSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""SubjectUserSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{userSessionToken}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""DatabaseName"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{dbName}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""DatabaseDescription"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : """",
                            ""N""  : false
                        }}]
            }}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var nodeName = $"{Program.NodeExecutableName} #{clientNodeNumber}";
            try
            {

                var response = Program.UnoSysApiConnection.PostAsync(content).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result = true;
                    // Retrieve result of call
                    //var responseJson = response.Content.ReadAsStringAsync().Result;
                    //using var doc = JsonDocument.Parse(responseJson);
                    //var element = doc.RootElement;
                    //result = element.GetProperty("O")[0].GetProperty("V").GetString()!;
                }
                else
                {
                    Program.DisplayUnsuccessfulWriteLine($"Error calling CallCreateDatabaseApiOnNode() - response.StatusCode={response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Program.DisplayUnsuccessfulWriteLine($"Error in CallCreateDatabaseApiOnNode(): {nodeName} - {ex} ");
            }
            return result;
        }


        private bool CallDeleteDatabaseApiOnNode(string userSessionToken, string dbName)
        {
            bool result = false;
            //string result = null!;
            var json = @$"
            {{
                ""V"" : 1,
                ""F"" : 0,
                ""A"" : {/*(int)ApiIdentifier.UserDeleteDatabase*/505},
                ""I"" : [{{
                            ""D"" : {{
                                        ""N"" : ""UserSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""SubjectUserSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{userSessionToken}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""DatabaseName"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{dbName}"",
                            ""N""  : false
                        }}]
            }}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var nodeName = $"{Program.NodeExecutableName} #{clientNodeNumber}";
            try
            {

                var response = Program.UnoSysApiConnection.PostAsync(content).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result = true;
                    // Retrieve result of call
                    //var responseJson = response.Content.ReadAsStringAsync().Result;
                    //using var doc = JsonDocument.Parse(responseJson);
                    //var element = doc.RootElement;
                    //result = element.GetProperty("O")[0].GetProperty("V").GetString()!;
                }
                else
                {
                    Program.DisplayUnsuccessfulWriteLine($"Error calling CallDeleteDatabaseApiOnNode() - response.StatusCode={response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Program.DisplayUnsuccessfulWriteLine($"Error in CallDeleteDatabaseApiOnNode(): {nodeName} - {ex} ");
            }
            return result;

        }


        private string CallConnectToDatabaseApiOnNode(string userSessionToken, string dbName, int desiredAccess, int shareMode)
        {
            string result = null!;
            var json = @$"
            {{
                ""V"" : 1,
                ""F"" : 0,
                ""A"" : {/*(int)ApiIdentifier.DatabaseConnect*/300},
                ""I"" : [{{
                            ""D"" : {{
                                        ""N"" : ""UserSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""SessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{userSessionToken}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""DatabaseName"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{dbName}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""DesiredAccess"",
                                        ""T"" : ""System.Int32"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{desiredAccess}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""ShareMode"",
                                        ""T"" : ""System.Int32"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{shareMode}"",
                            ""N""  : false
                        }}]
            }}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var nodeName = $"{Program.NodeExecutableName} #{clientNodeNumber}";
            try
            {

                var response = Program.UnoSysApiConnection.PostAsync(content).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    // Retrieve result of call
                    var responseJson = response.Content.ReadAsStringAsync().Result;
                    using var doc = JsonDocument.Parse(responseJson);
                    var element = doc.RootElement;
                    result = element.GetProperty("O")[0].GetProperty("V").GetString()!;
                }
                else
                {
                    Program.DisplayUnsuccessfulWriteLine($"Error calling CallConnectToDatabaseApiOnNode() - response.StatusCode={response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Program.DisplayUnsuccessfulWriteLine($"Error in CallConnectToDatabaseApiOnNode(): {nodeName} - {ex} ");
            }
            return result;

        }


        private bool CallDisconnectFromDatabaseApiOnNode(string userSessionToken, string dbSessionToken)
        {
            bool result = false;
            var json = @$"
            {{
                ""V"" : 1,
                ""F"" : 0,
                ""A"" : {/*(int)ApiIdentifier.DatabaseDisconnect*/301},
                ""I"" : [{{
                            ""D"" : {{
                                        ""N"" : ""UserSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""DatabaseSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{dbSessionToken}"",
                            ""N""  : false
                        }}]
            }}";

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var nodeName = $"{Program.NodeExecutableName} #{clientNodeNumber}";
            try
            {

                var response = Program.UnoSysApiConnection.PostAsync(content).Result;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    result = true;
                }
                else
                {
                    Program.DisplayUnsuccessfulWriteLine($"Error calling CallDisconnectToDatabaseApiOnNode() - response.StatusCode={response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Program.DisplayUnsuccessfulWriteLine($"Error in CallDisconnectToDatabaseApiOnNode(): {nodeName} - {ex} ");
            }
            return result;
        }
        #endregion
    }


}
