using Microsoft.Extensions.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Unosys.SDK;
using UnoSys.Api.Models;
using CsvHelper;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;
using UnoSys.Api.Exceptions;
using System.Reflection.Emit;
using System.Dynamic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Net.Http.Headers;

namespace WorldComputer.Simulator
{

    public class DataSetCommandContext : ICommandContext
	{
        #region Field Members
        const int DEFAULT_CLIENT_NODE_NUMBER = 1;
        const int MAX_TITLE_LENGTH = 200;

        Hashtable switchSet = new Hashtable( 20 );
		string[] cmdArgs;
        string userName = null!;
        bool fieldsSwitch = false;
        //bool indexSwitch = false;
        bool userSwitch = false;
        //bool whereSwitch = false;
        bool publishSwitch = false;
        bool accessSwitch = false;
        bool clientSimulator = false;
        bool simulatorHelp = false;
        bool outputSwitch = false;
        bool countOnlySwitch = false;
        string contentPrice = null!;
        string fieldsList = null!;
        string indexFieldsList = null!;
        string contentFile = null!;
        string contentTitle = null!;
        string outputFilePath = null!;
        string whereClause = null!;
        byte[] whereClauseAssembly = null!;
        Type whereClauseEvaluatorType = null!;
        NetworkSpec NetworkSpec = null!;
        VDriveInfo VirtualDriveSpec = null!;
        int clientNodeNumber = DEFAULT_CLIENT_NODE_NUMBER;

		#endregion

		#region Constructors
		public DataSetCommandContext( string[] commandContextArgs )
		{
            cmdArgs = commandContextArgs;
			// Define valid context switches
			string[] allowedSwitches = new string[8];
            allowedSwitches[0] = "/ACCESS";
            allowedSwitches[1] = "/CLIENTNODE:";
            allowedSwitches[2] = "/COUNTONLY";
            allowedSwitches[3] = "/FIELDS:";
            //allowedSwitches[4] = "/INDEX:";
            allowedSwitches[4] = "/OUTPUT:";
            allowedSwitches[5] = "/PUBLISH:";
            allowedSwitches[6] = "/USER:";
           // allowedSwitches[8] = "/WHERE:";
            allowedSwitches[7] = "/?";
			CommandLineTool.ParseCommandContextSwitches( commandContextArgs, allowedSwitches, switchSet, this );
			simulatorHelp = (bool) switchSet["/?"];
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
                Program.UnoSysApiConnection = new UnoSysConnection(string.Format(Program.UnoSysApiUrlTemplate,Program.BasePort + ((clientNodeNumber - 1) * Program.BasePortSpacing)));

                #region Publish content
                if (publishSwitch)
                {
                   PublishContent();
                }
                #endregion

                #region Access content
                if (accessSwitch)
                {
                    AccessContent();
                }
                #endregion
            }
        }

        public bool ValidateContext()
        {
            
            bool invalidSwitch = false;
            object switchValue = null;

            #region Switch Processing
            #region Publish switch processing
            //Examples of possible legal switch usage: /PUBLISH
            switchValue = switchSet["/PUBLISH"];
            if (switchValue is bool)
            {
                publishSwitch = (bool)switchValue;
            }
            #endregion

            #region Access switch processing
            //Examples of possible legal switch usage: /ACCESS
            switchValue = switchSet["/ACCESS"];
            if (switchValue is bool)
            {
                accessSwitch = (bool)switchValue;
            }
            #endregion

            #region CountOnly switch processing
            //Examples of possible legal switch usage: /COUNTONLY
            switchValue = switchSet["/COUNTONLY"];
            if (switchValue is bool)
            {
                countOnlySwitch = (bool)switchValue;
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

            #region Fields switch processing
            switchValue = switchSet["/FIELDS"];
            if (switchValue is string)
            {
                if (!string.IsNullOrEmpty((string)switchValue))
                {
                    fieldsList = ((string)switchValue).ToUpper();
                    fieldsSwitch = true;
                }
            }
            else
            {
                if ((bool)switchValue)
                {
                    fieldsSwitch = true;
                }
            }
            #endregion


            //#region Index switch processing
            //switchValue = switchSet["/INDEX"];
            //if (switchValue is string)
            //{
            //    if (!string.IsNullOrEmpty((string)switchValue))
            //    {
            //        indexFieldsList = ((string)switchValue).ToUpper();
            //        indexSwitch = true;
            //    }
            //}
            //else
            //{
            //    if ((bool)switchValue)
            //    {
            //        indexSwitch = true;
            //    }
            //}
            //#endregion


            #region Output switch processing
            switchValue = switchSet["/OUTPUT"];
            if (switchValue is string)
            {
                if (!string.IsNullOrEmpty((string)switchValue))
                {
                    outputFilePath = ((string)switchValue).ToUpper();
                    outputSwitch = true;
                }
            }
            else
            {
                if ((bool)switchValue)
                {
                    outputSwitch = true;
                }
            }
            #endregion


            //#region Where switch processing
            //switchValue = switchSet["/WHERE"];
            //if (switchValue is string)
            //{
            //    if (!string.IsNullOrEmpty((string)switchValue))
            //    {
            //        whereClause = ((string)switchValue).ToUpper();
            //        whereSwitch = true;
            //    }
            //}
            //else
            //{
            //    if ((bool)switchValue)
            //    {
            //        whereSwitch = true;
            //    }
            //}
            //#endregion

            #endregion


            #region Invalid Switch Combination Check
            if (publishSwitch && accessSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/PUBLISH and /ACCESS cannot be used together.");
            }
            if (fieldsSwitch && (!publishSwitch && !accessSwitch))
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/FIELDS must be used with either /PUBLISH or /ACCESS.");
            }
            if (outputSwitch && !accessSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/OUTPUT can only be used with /ACCESS.");
            }
            //if (whereSwitch && !accessSwitch)
            //{
            //    throw new CommandLineToolInvalidSwitchCombinationException("/WHERE can only be used with /ACCESS.");
            //}
            if (countOnlySwitch && !accessSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/COUNTONLY can only be used with /ACCESS.");
            }
            //if (indexSwitch && accessSwitch)
            //{
            //    throw new CommandLineToolInvalidSwitchCombinationException("/INDEX cannot be used with /ACCESS.");
            //}
            if (!publishSwitch && !simulatorHelp && !accessSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("One of /PUBLISH or /ACCESS is required.");
            }
            #endregion

            bool result = (fieldsSwitch /*|| indexSwitch*/ | publishSwitch || accessSwitch || userSwitch || outputSwitch /*|| whereSwitch*/ || countOnlySwitch ||  clientSimulator || simulatorHelp);
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
			Program.DisplaySuccessfulWriteLine( "World Computer DATASET command usage:" );
            Program.DisplaySuccessfulWriteLine( "" );
            Program.DisplaySuccessfulWriteLine( "      WCSim DATASET | DS  [<switches>]" );
			Program.DisplaySuccessfulWriteLine( "" );
			Program.DisplaySuccessfulWriteLine( "where <switches> are one or more of:" );
			Program.DisplaySuccessfulWriteLine( "" );
            Program.DisplaySuccessfulWriteLine( " /ACCESS\t\t\t\tAccess a published Data Set");
            Program.DisplaySuccessfulWriteLine($" /CLIENTNODE:<Node#>\t\t\tClient Node # to connect to - ({DEFAULT_CLIENT_NODE_NUMBER})");
            Program.DisplaySuccessfulWriteLine( " /COUNTONLY\t\t\t\tCount the records that would result rather than output them");
            Program.DisplaySuccessfulWriteLine( " /FIELDS:<field>,...\t\t\tList of one or more fields to process - (All)");
            //Program.DisplaySuccessfulWriteLine( " /INDEX:<field>,...\t\t\tList fields to create an Index for - (All)");
            Program.DisplaySuccessfulWriteLine( " /OUTPUT:<filepath>\t\t\tOutput *.CSV file path");
            Program.DisplaySuccessfulWriteLine( " /PUBLISH\t\t\t\tPublish a User's Data Set");
            Program.DisplaySuccessfulWriteLine( " /USER:<UserName>\t\t\tEstablish logged in User context to execute operation in");
            //Program.DisplaySuccessfulWriteLine( " /WHERE:<clause>\t\t\tQuery expression records must meet to be included in result");
            Program.DisplaySuccessfulWriteLine( " /?\t\t\t\t\tUsage information" );
			Program.DisplaySuccessfulWriteLine( "" );
            Program.DisplaySuccessfulWriteLine("=======================================================================================================");
        }
		#endregion


		#region Helpers
        private void PublishContent()
        {
            UserSessionInfo userSessionInfo = null!;

            #region  Step #1:  Check if a simulation has already been created and if noto error
            if (!Simulator.NetworkAlreadyCreated())
            {
                throw new CommandLineToolInvalidOperationException($"A World Computer node network does not exist. Use 'WCSim NODE /Create' to create one first.");
            }
            #endregion

            #region Step #2: Deserialize the SIMULATOR_SETTINGS_FILE_NAME settings file
            NetworkSpec = Simulator.GetNetworkSpec();//)JsonSerializer.Deserialize<NetworkSpec>(fs);
            //using (System.IO.FileStream fs = new System.IO.FileStream(System.IO.Path.Combine(Program.WorkingDir, Simulator.SIMULATOR_SETTINGS_FILE_NAME), 
            //    System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            //{
            //    NetworkSpec = (NetworkSpec)JsonSerializer.Deserialize<NetworkSpec>(fs);
            //}
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
            if (!userLoggedIn )
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
                    #region Step #8: Deserialize UserSessionInfo to obtain the Token of the Logged in User session
                    userSessionInfo = Simulator.GetUserSessionInfo(clientNodeNumber, userName);
                    if (userSessionInfo == null) 
                    {
                        throw new CommandLineToolInvalidOperationException($"Corrupted WCSim user file.");
                    }
                    userName = userSessionInfo.UserName;
                    #endregion
                }

                #region Step #9 - Prompt User for DataSet filename 
                var ogrForegroundColor = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("Enter path to CSV Data Set file to Publish: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    contentFile = Console.ReadLine();
                    //contentFile = "C:\\WorldComputer\\Hackathons\\Hedera2025a\\USZipCodes.csv"; // For Now
                    //contentFile = "C:\\WorldComputer\\Hackathons\\Hedera2025a\\CanadianPostalCodes202403.csv"; // For Now
                    //contentFile = "C:\\WorldComputer\\Hackathons\\Hedera2025a\\USZIPCodes202507.csv"; // For Now
                    //contentFile = "C:\\WorldComputer\\Hackathons\\Hedera2025a\\PowerBallLottery2.csv"; // For Now
                    
                    if (!System.IO.File.Exists(contentFile))
                    {
                        Program.DisplayUnsuccessfulWriteLine($"Data Set to Publish: '{contentFile}' cannot be found.");
                        throw new CommandLineToolInvalidSwitchException($"Data Set to Publish: '{contentFile}' cannot be found.");
                    }
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("");
                    Console.WriteLine("Enter Title:");
                    Console.ForegroundColor = ConsoleColor.White;
                    contentTitle = Console.ReadLine();
                    //contentTitle = "US Zip Codes"; // For Now
                    if (string.IsNullOrEmpty(contentTitle))
                    {
                        contentTitle = "*** No Title Provided ***";
                    }
                    if(contentTitle.Length > MAX_TITLE_LENGTH)
                    {
                        contentTitle = contentTitle.Substring(0, MAX_TITLE_LENGTH);
                    }
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("");
                    Console.WriteLine("Enter USD Price to access row of Data Set (empty for 0.00): $");
                    Console.ForegroundColor = ConsoleColor.White;
                    contentPrice = Console.ReadLine();
                    //contentPrice = "0.0001"; // For Now
                    if ( string.IsNullOrEmpty(contentPrice))
                    {
                        contentPrice = "0.00";
                    }
                    else
                    {
                        contentPrice = contentPrice.Trim();
                        decimal price = 0.00m;
                        if(!decimal.TryParse(contentPrice, out price))
                        {
                            throw new CommandLineToolInvalidSwitchException($"Invalid Price format.");
                        }
                        int decPos = contentPrice.IndexOf(".");
                        if ( decPos >= 0 && decPos < contentPrice.Length - 6)
                        {
                            throw new CommandLineToolInvalidSwitchException($"Invalid Price format - cannot have more than 5 decimal places.");
                        }
                    }

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("");
                    Console.WriteLine("Enter list of fields to index (e.g.; field1,field7) - empty to index all fields: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    indexFieldsList = Console.ReadLine();
                    #region  Step #10:  - Open and Validate CSV file and Create Table Schema
                    //TableSchema tableSchema = null!;
                    Unosys.Common.Types.SetSchema setSchema = null!;
                    ulong totalRecords = 0;
                    try
                    {
                        using (var reader = new System.IO.StreamReader(contentFile))
                        {
                            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                            {
                                csv.Read();
                                csv.ReadHeader();
                                var headerRecord = csv.HeaderRecord;
                                Dictionary<string, int> maxColumnSize = new Dictionary<string, int>();
                                #region Validate any fieldList provided
                                ValidateFields(headerRecord);
                                #endregion 
                                #region Validate any indexFieldList provided
                                ValidateIndexFields(headerRecord);
                                #endregion


                                foreach (var column in headerRecord)
                                {
                                    maxColumnSize.Add(column, 1);
                                }

                                var records = csv.GetRecords<dynamic>();
                                foreach (IDictionary<string, object?> rec in records)
                                {
                                    //IDictionary<string, object?> rec = record;
                                    //Console.WriteLine(rec[headerRecord[0]]);
                                    for (int i = 0; i < headerRecord.Length; i++)
                                    {
                                        if (((string)rec[headerRecord[i]]).Length > maxColumnSize[headerRecord[i]])
                                        {
                                            maxColumnSize[headerRecord[i]] = ((string)rec[headerRecord[i]]).Length;
                                        }
                                    }
                                    totalRecords++;
                                }
                                DateTime version = DateTime.UtcNow;
                                Dictionary<int, Unosys.Common.Types.FieldDef>? fields = new Dictionary<int, Unosys.Common.Types.FieldDef>();
                                Dictionary<int, Unosys.Common.Types.OrderDef>? orders = new Dictionary<int, Unosys.Common.Types.OrderDef>();
                                int orderCount = 1;
                                int columnIndex = 1;  // Fields are indexed starting at 1, so we start at 1 and leave 0 for the "ROWID" field
                                foreach (var column in maxColumnSize)
                                {
                                    if (fieldsList != null && !fieldsList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Contains(column.Key.ToUpper()))
                                    {
                                        continue; // Skip this column if it is not in the fields list
                                    }
                                    fields[columnIndex] = new Unosys.Common.Types.FieldDef
                                    {
                                        Name = column.Key.ToUpper(),
                                        TypeDefinition = Unosys.Common.Types.TypeDef.Factory(Unosys.Common.Types.Type.CHARACTER),
                                        Length = column.Value,
                                        Decimals = 0,
                                        IsNullable = false,
                                        IsMultiValued = false,
                                        FieldEncoding = Unosys.Common.Types.TypeEncoding.UTF8,
                                        DefaultValue = "",
                                        Description = "",
                                        Ordinal = columnIndex,
                                    };
                                    //orders[orderCount++] = new Unosys.Common.Types.OrderDef(new List<byte> { (byte)columnIndex });
                                    columnIndex++;
                                }
                                #region Process Indexes
                                columnIndex = 1;  // Fields are indexed starting at 1, so we start at 1 and leave 0 for the "ROWID" field
                                int fldCount = 0;
                                foreach (var column in maxColumnSize)
                                {
                                    fldCount++;
                                    if (string.IsNullOrEmpty(indexFieldsList) || !indexFieldsList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Contains(column.Key.ToUpper()))
                                    {
                                        continue; // Skip this column if it is not in the index fields list
                                    }
                                    orders[orderCount++] = new Unosys.Common.Types.OrderDef(new List<byte> { (byte)fldCount });
                                    columnIndex++;
                                }
                                #endregion 
                                setSchema = new Unosys.Common.Types.SetSchema(1, fields, orders);
                            }
                        }
                    }
                    catch (Exception)
                    {

                        throw;
                    }
                    #endregion 
                    if (setSchema != null)
                    {
                        // Create the content file's name on the Virtual Drive
                        var wcContentFileName = System.IO.Path.Combine( "WCOPublishedDataSets",Guid.NewGuid().ToString("N").ToUpper() + contentPrice.PadLeft(16, ' ').Replace(".", "_") + "_" + contentTitle.Trim() + $".{userName}.tbl");
                        Console.WriteLine("");
                        Console.WriteLine("Publishing your Data Set content... One Moment Please...");
                        Console.WriteLine("");
                        API.Initialize(new Guid(VirtualDriveSpec.VirtualDiskSessionToken.Substring(1)),
                                        new Guid(VirtualDriveSpec.VolumeSessionToken.Substring(1)),
                                        userSessionInfo.UserSessionToken);

                        #region Step #11 - Write Data Set to Virtual Drive one record at a time
                        var watch = Stopwatch.StartNew();
                        int recCount = 0;
                        using (var theTable = Table.Create(wcContentFileName, setSchema, (uint)Unosys.SDK.FileAccess.ReadWrite, (uint)Unosys.SDK.FileAttributes.Normal, (uint)Unosys.SDK.FileShare.None))
                        {
                            using (var reader = new System.IO.StreamReader(contentFile))
                            {
                                
                                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                                {
                                    csv.Read();
                                    csv.ReadHeader();
                                    var headerRecord = csv.HeaderRecord;
                                    var records = csv.GetRecords<dynamic>();

                                    foreach (IDictionary<string, object?> rec in records)
                                    {
                                        recCount++;
                                        theTable.Append();
                                        for (int i = 0; i < headerRecord.Length; i++)
                                        {
                                            if (theTable.FieldNameExists(headerRecord[i]))
                                            {
                                                theTable[headerRecord[i]] = ((string)rec[headerRecord[i]]);
                                            }
                                            //Console.Write($"\t{theTable.FieldName(i+1)} = {theTable[i+1]}");
                                        }

                                        theTable.Commit();
                                        //if (recCount == 200)
                                        //{
                                        //    break; // For Now - just write the first 2 records}
                                        //}
                                        if (recCount % 100 == 0)
                                        {
                                            Console.WriteLine($"{recCount} of {totalRecords} Data Set records successfully imported...");
                                        }
                                    }
                                }
                                Console.WriteLine();
                                Program.DisplaySuccessfulWriteLine($"{recCount} Data Set records successfully published.");
                                Console.WriteLine();
                            }
                            //Console.WriteLine($"Sanity Check:  ROWID={theTable["$RID$"]}, ZIPCODE={theTable["ZIPCODE"]}");
                            //Console.WriteLine($"Sanity Check:  ROWID={theTable["$RID$"]}, POSTAL_CODE={theTable["POSTAL_CODE"]}");
                        }
                        watch.Stop();
                        #endregion


                        //using (var theTable = Table.Open(wcContentFileName, (uint)Unosys.SDK.FileAccess.ReadWrite, (uint)Unosys.SDK.FileAttributes.Normal, (uint)Unosys.SDK.FileShare.None))
                        //{
                        //    int recCount = 1;
                        //    theTable.SetOrder("LAT"); // Set to first order (i.e.; first field)
                        //                                         //theTable.SeekExact("14 16 37 48 58 18"); // Seek to the first record with the value "15 44 63 66 69 20" in the second field (i.e.; WINNINGNUMBERS)
                        //    theTable.SeekClosest("17"); // Seek to the first record with the value "15 44 63 66 69 20" in the second field (i.e.; WINNINGNUMBERS)
                        //                                //theTable.GoTop();  // Go to the logical top of the table (respecting the current order if any)
                        //    while (!theTable.IsEoT)
                        //    {
                        //        Console.Write($"{recCount}.");
                        //        for (int i = 1; i <= theTable.FieldCount; i++)
                        //        {
                        //            Console.Write($"\t{theTable.FieldName(i)} = {theTable[i]}");
                        //        }
                        //        Console.WriteLine();
                        //        recCount++;
                        //        theTable.MoveNext();
                        //    }

                        //    Console.WriteLine($"Total Records in Table: {recCount - 1} - press any key to list the records again in next order");
                        //    Console.ReadLine();

                        //}

                        // var result = CallGeneralLedgerDefundWalletApiOnNode(userSessionInfo.UserSessionToken, dltAddress, dltPrivateKey, fundsAmount);
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine($"Data Set Content Title: '{contentTitle}' successfully published to the Virtual Drive");
                        Console.WriteLine($"Total records imported: {recCount}");
                        Console.WriteLine($"Total time: {watch.Elapsed.TotalSeconds} seconds.");
                    }
                    else
                    {
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine($"Data Set Content Title: '{contentTitle}' not created on the Virtual Drive - Invalid or missing Table Schema");
                    }
                }
                catch (AggregateException aex)
                {
                    if( aex.InnerExceptions.Count > 0 && aex.InnerExceptions[0] is UnoSysArgumentException)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine($"Data Set Content Title: '{contentTitle}' not created on the Virtual Drive - {aex.InnerExceptions[0].Message}");
                    }
                    else
                    {
                        Console.WriteLine("");
                        Console.WriteLine("");
                        Console.WriteLine($"Data Set Content Title: '{contentTitle}' not created on the Virtual Drive");
                    }
                }

                catch (Exception)
                {
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine($"Data Set Content Title: '{contentTitle}' not created on the Virtual Drive");
                }
                finally
                {
                    Console.ForegroundColor = ogrForegroundColor;
                }
                #endregion 


               
            }
            #endregion

            //#region Step #7 - Display result
            //Program.DisplaySuccessfulWriteLine($"");
            ////Program.DisplaySuccessfulWriteLine($"REPORTS for {userSessionInfo.UserName}: ");
            ////Program.DisplaySuccessfulWriteLine("==================================================================================");
            ////Program.DisplaySuccessfulWriteLine($"");
            //Program.DisplaySuccessfulWriteLine(result);
            //#endregion

        }

 
        private void AccessContent()
        {
            
            UserSessionInfo userSessionInfo = null!;

            #region  Step #1:  Check if a simulation has already been created and if noto error
            if (!Simulator.NetworkAlreadyCreated())
            {
                throw new CommandLineToolInvalidOperationException($"A World Computer node network does not exist. Use 'WCSim NODE /Create' to create one first.");
            }
            #endregion

            #region Step #2: Deserialize the SIMULATOR_SETTINGS_FILE_NAME settings file
            NetworkSpec = Simulator.GetNetworkSpec();//)JsonSerializer.Deserialize<NetworkSpec>(fs);
            //using (System.IO.FileStream fs = new System.IO.FileStream(System.IO.Path.Combine(Program.WorkingDir, Simulator.SIMULATOR_SETTINGS_FILE_NAME), 
            //    System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
            //{
            //    NetworkSpec = (NetworkSpec)JsonSerializer.Deserialize<NetworkSpec>(fs);
            //}
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
                throw new CommandLineToolInvalidOperationException($"A World Computer Virtual Drive does not exist. Use 'VDRIVE /Create' to create a one first.");
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
                    if (userSessionInfo == null)
                    {
                        throw new CommandLineToolInvalidOperationException($"Corrupted WCSim user file.");
                    }
                    userName = userSessionInfo.UserName;
                    #endregion
                }
                
                #region Step #8 - Fetch list of published Content to Present to User
                var vDiskRootFolderName = $"Node{clientNodeNumber}_Root_{new Guid(VirtualDriveSpec.VirtualDiskSessionToken.Substring(1)).ToString("N").ToUpper()}";
                var vDiskRootPath = System.IO.Path.Combine(@".\LocalStore", vDiskRootFolderName);
                var contentDictionares = System.IO.Directory.GetDirectories(vDiskRootPath, "*WCOPublishedDataSets", System.IO.SearchOption.TopDirectoryOnly);
                var contentFiles = System.IO.Directory.GetFiles(contentDictionares[0], "*.tbl");
                #endregion 

                #region Step #9 - Prompt User for Content to Access 
                var ogrForegroundColor = Console.ForegroundColor;
                try
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("Enter the # of the Data Set to access: ");
                    decimal decPrice = 0.00m;
                    string[] ContentDIDRefs = new string[contentFiles.Length];
                    string[] ContentFileNames = new string[contentFiles.Length];
                    string[] Prices = new string[contentFiles.Length];
                    string[] Titles = new string[contentFiles.Length];
                    string[] UserNames = new string[contentFiles.Length];
                    string contentIndex = null!;
                    bool noSelectionMade = false;
                    while (true)
                    {
                        for (int index = 0; index < contentFiles.Length; index++)
                        {
                            var absoluteFileName = System.IO.Path.GetFileName(contentFiles[index]);
                            ContentFileNames[index] = absoluteFileName;
                            //ContentFileNames[index] = ConvertBytesToHexString(Encoding.Unicode.GetBytes(absoluteFileName));
                            //var x = Encoding.Unicode.GetString(ConvertHexStringToBytes(ContentFileNames[index]));
                            var contentFileName = System.IO.Path.GetFileName(contentFiles[index]);
                            ContentDIDRefs[index] = contentFileName.Substring(0, 32);
                            Prices[index] = contentFileName.Substring(32, 16).Replace("_", ".");
                            var pathWithoutCntExtension = System.IO.Path.GetFileNameWithoutExtension(contentFileName);
                            UserNames[index] = System.IO.Path.GetExtension(pathWithoutCntExtension).Substring(1);
                            var contentFileProper = System.IO.Path.GetFileNameWithoutExtension(pathWithoutCntExtension);
                            Titles[index] = contentFileProper.Substring(49);
                            decPrice = decimal.Parse(Prices[index]);
                            Console.WriteLine($"{(index+1).ToString().PadLeft(3, ' ')}. {(decPrice == 0.00m ? "free" : "$ " + decPrice.ToString("F5")).TrimEnd('0')}  {"\t\t"+Titles[index]}");
                            //Console.WriteLine($"{(index + 1).ToString().PadLeft(3, ' ')}. {(decPrice == 0.00m ? "free" : "$ " + decPrice.ToString("F2"))}  {"\t\t" + Titles[index]}");
                            //    var absoluteFileName = System.IO.Path.GetFileName(contentFiles[index]).Substring(37);
                            //    ContentFileNames[index] = absoluteFileName;
                            //    var contentFileName = System.IO.Path.GetFileName(contentFiles[index]).Substring(37);
                            //    ContentDIDRefs[index] = contentFileName.Substring(0, 32);
                            //    Prices[index] = contentFileName.Substring(32, 16).Replace("_","."); 
                            //    var pathWithoutCntExtension = System.IO.Path.GetFileNameWithoutExtension(contentFileName);
                            //    UserNames[index] = System.IO.Path.GetExtension(pathWithoutCntExtension).Substring(1);
                            //    var contentFileProper = System.IO.Path.GetFileNameWithoutExtension(pathWithoutCntExtension);
                            //    Titles[index] = contentFileProper.Substring(49);
                            //    decPrice = decimal.Parse(Prices[index]);
                            //    Console.WriteLine($"{(index+1).ToString().PadLeft(3, ' ')}. {(decPrice == 0.00m ? "free" : "$ " + decPrice.ToString("F5")).TrimEnd('0')}  {"\t\t"+Titles[index]}");
                        }
                        Console.WriteLine($"{"Q".PadLeft(3, ' ')}. Quit");
                        Console.ForegroundColor = ConsoleColor.White;
                        contentIndex = Console.ReadLine();


                        if (contentIndex.ToUpper() == "Q")
                        {
                            noSelectionMade = true;
                            break;
                        }
                        else
                        {
                            int index = 0;
                            if( !int.TryParse(contentIndex, out index) || index < 1 || index > contentFiles.Length)
                            {
                                Console.ForegroundColor = ConsoleColor.Red;
                                Console.WriteLine("");
                                Console.WriteLine("Invalid selection....please try again.");
                                Console.WriteLine("");
                                Console.WriteLine("");
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine("Enter the # of the Data Set to access: ");
                                continue;
                            }
                           


                            #region Validate Fields List
                            ValidateAccessFields(userSessionInfo, ContentFileNames, index);
                            #endregion

                            #region Obtain and validate Data Set selection criteria Where clause
                            Console.ForegroundColor = ConsoleColor.Cyan;
                            Console.WriteLine("");
                            Console.WriteLine("Enter any data selection criteria (e.g.; field1 >= 'value' and field1 <= 'value2') - empty for All records: ");
                            Console.ForegroundColor = ConsoleColor.White;
                            whereClause = Console.ReadLine();
                            ValidateWhereClause(userSessionInfo, ContentFileNames, index);
                            #endregion 


                            decPrice = decimal.Parse(Prices[index-1]);
                            int recCount = 0;
                            //Console.WriteLine($"A - {decPrice > 0.00m},  {UserNames[index - 1].ToUpper()}, {userName.ToUpper()}, {!(UserNames[index - 1].ToUpper().Equals(userName.ToUpper()))}");
                            if (decPrice > 0.00m && !(UserNames[index-1].ToUpper().Equals(userName.ToUpper())) && !countOnlySwitch)
                            {
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"Your Data Set selection costs ${decPrice.ToString("F5")} per retrieved record to access.  Would you like to access it (Y/N)?");
                                Console.ForegroundColor = ConsoleColor.White;
                                var answer = Console.ReadKey();
                                string result = null!;
                                if (answer.KeyChar == 'Y' || answer.KeyChar == 'y')
                                {
                                    Console.WriteLine("");
                                    Console.WriteLine("");
                                    Console.WriteLine($"{(countOnlySwitch ? "Counting" : "Accessing")} your selected Data Set - One Moment Please...");
                                    //#region Call GeneralLedgerPurchaseContent on API
                                    ////result = CallGeneralLedgerPurchaseContentOnAPI(userSessionInfo.UserSessionToken, ContentDIDRefs[index - 1], decPrice);
                                    ////if (result == null)
                                    ////{
                                    ////    break;
                                    ////}
                                    //Console.WriteLine("");
                                    //Console.WriteLine("");
                                    //Console.WriteLine(result);
                                    //Console.WriteLine("");
                                    //#endregion
                                    var watch = Stopwatch.StartNew();
                                    recCount = AccessDataSet(userSessionInfo, ContentFileNames, index);
                                    if (recCount == 0)
                                    {
                                        Console.WriteLine($"Zero records found matching your criteria.");
                                    }
                                    else
                                    {
                                        Console.WriteLine($"Total # of Records found: {recCount}.");
                                    }
                                    watch.Stop();
                                    Console.WriteLine($"Total time: {(watch.Elapsed.TotalSeconds == 0? watch.Elapsed.TotalMilliseconds : watch.Elapsed.TotalSeconds)} {(watch.Elapsed.TotalSeconds == 0 ? "milliseconds" : "seconds")}.");
                                    if (!countOnlySwitch && recCount > 0)
                                    {                                       
                                        Console.WriteLine($"Total cost: ${recCount * decPrice}");
                                        #region Call GeneralLedgerPurchaseContent on API
                                        Console.WriteLine($"Completing payment...One Moment Please...");
                                        result = CallGeneralLedgerPurchaseDataSetOnAPI(userSessionInfo.UserSessionToken, ContentDIDRefs[index - 1], recCount * decPrice);
                                        if (result == null)
                                        {
                                            break;
                                        }
                                        Console.WriteLine("");
                                        Console.WriteLine("");
                                        Console.WriteLine(result);
                                        Console.WriteLine($"(Current Exchange rate used: {CallGeneralLedgerGetExchangeRateOnAPI().ToString()} USD per HBar)");
                                        Console.WriteLine("");

                                        #endregion
                                    }
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("");
                                    Console.WriteLine("");
                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine("Enter the # of the Data Set to access: ");
                                    continue;
                                }
                            }
                            else  // Free data sets or /COUNTONLY switch
                            {
                                Console.WriteLine("");
                                Console.WriteLine("");
                                Console.WriteLine($"{(countOnlySwitch ? "Counting" : "Accessing")} your selected Data Set - One Moment Please...");
                                Console.WriteLine("");

                                var watch = Stopwatch.StartNew();
                                recCount = AccessDataSet(userSessionInfo, ContentFileNames, index); 
                                if( recCount == 0)
                                {
                                    Console.WriteLine($"Zero records found matching your criteria.");
                                }
                                else
                                {
                                    Console.WriteLine($"Total # of Records found: {recCount}.");
                                }
                                watch.Stop();
                                Console.WriteLine($"Total time: {(watch.Elapsed.TotalSeconds == 0 ? watch.Elapsed.TotalMilliseconds : watch.Elapsed.TotalSeconds)} {(watch.Elapsed.TotalSeconds == 0 ? "milliseconds" : "seconds")}.");
                                if(decPrice > 0.00m && !(UserNames[index - 1].ToUpper().Equals(userName.ToUpper())))
                                {
                                    Console.WriteLine($"The records would cost: ${recCount * decPrice} to access.");
                                }
                                break;
                            }
                        }
                    }

                    #region Step #10 - Call CallGeneralLedgerDefundWalletApiOnNode
                    // var result = CallGeneralLedgerDefundWalletApiOnNode(userSessionInfo.UserSessionToken, dltAddress, dltPrivateKey, fundsAmount);
                    Console.WriteLine("");
                    Console.WriteLine("");
                    if(noSelectionMade)
                        Console.WriteLine($"No Data Set selection was made.");
                    else
                        Console.WriteLine($"");
                    #endregion
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{ex.Message}");
                    throw;
                }
                finally
                {
                    Console.ForegroundColor = ogrForegroundColor;
                }
                #endregion
            }
            #endregion

            //#region Step #7 - Display result
            //Program.DisplaySuccessfulWriteLine($"");
            ////Program.DisplaySuccessfulWriteLine($"REPORTS for {userSessionInfo.UserName}: ");
            ////Program.DisplaySuccessfulWriteLine("==================================================================================");
            ////Program.DisplaySuccessfulWriteLine($"");
            //Program.DisplaySuccessfulWriteLine(result);
            //#endregion

        }

        private void ValidateFields(string[] headerRecord)
        {
            if (fieldsList != null)
            {
                string[] flds = fieldsList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string field in flds)
                {
                    bool fieldExists = false;
                    foreach (var column in headerRecord)
                    {
                        if (column.Trim().Equals(field, StringComparison.OrdinalIgnoreCase))
                        {
                            fieldExists = true;
                            break;
                        }
                    }
                    if (!fieldExists)
                    {
                        Program.DisplayUnsuccessfulWriteLine($"Unknown /FIELDS field specified '{field.ToUpper()}'.");
                        throw new CommandLineToolInvalidCommandException($"Unknown /FIELDS field specified '{field.ToUpper()}'.");
                    }
                }
            }
        }

        private void ValidateIndexFields(string[] headerRecord)
        {
            if (!string.IsNullOrEmpty(indexFieldsList))
            {
                string[] indexFields = indexFieldsList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string field in indexFields)
                {
                    bool fieldExists = false;
                    foreach (var column in headerRecord)
                    {
                        if (column.Trim().Equals(field, StringComparison.OrdinalIgnoreCase))
                        {
                            fieldExists = true;
                            break;
                        }
                    }
                    if (!fieldExists)
                    {
                        Program.DisplayUnsuccessfulWriteLine($"Unknown Index field specified '{field.ToUpper()}'.");
                        throw new CommandLineToolInvalidCommandException($"Unknown Index field specified '{field.ToUpper()}'.");
                    }

                    #region Ensure Index field is included in the fieldsList provided
                    if (fieldsList != null && !fieldsList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Contains(field.ToUpper()))
                    {
                        Program.DisplayUnsuccessfulWriteLine($"Index field '{field.ToUpper()}' must be included in the /FIELDS list.");
                        throw new CommandLineToolInvalidCommandException($"Index field '{field.ToUpper()}' must be included in the /FIELDS list.");
                    }
                    #endregion
                }
            }
        }


        private int AccessDataSet(UserSessionInfo userSessionInfo, string[] ContentFileNames, int index)
        {
            int recCount = 0;
            var wcContentFileName = System.IO.Path.Combine("WCOPublishedDataSets", ContentFileNames[index - 1]);

            API.Initialize(new Guid(VirtualDriveSpec.VirtualDiskSessionToken.Substring(1)),
                       new Guid(VirtualDriveSpec.VolumeSessionToken.Substring(1)),
                       userSessionInfo.UserSessionToken);


            //var writeRecords = new List<System.Object>();
            using (dynamic theTable = Table.Open(wcContentFileName, (uint)Unosys.SDK.FileAccess.ReadWrite, (uint)Unosys.SDK.FileAttributes.Normal, (uint)Unosys.SDK.FileShare.None))
            {
                #region Position to starting record
                if (whereClauseEvaluatorType != null)
                {
                    whereClauseEvaluatorType.GetMethod("SetOrder")!.Invoke(null, new object[] { theTable });
                    whereClauseEvaluatorType.GetMethod("PositionToFirstRecord")!.Invoke(null, new object[] { theTable });
                }
                else
                {
                    theTable.GoTop();  // Go to the logical top of the table (respecting the current order if any)
                }
                #endregion 

                #region Determine fields to process
                List<int> fieldsToProcess = new List<int>();
                if (fieldsList == null!)
                {
                    for (int i = 1; i <= theTable.FieldCount; i++)
                    {
                        fieldsToProcess.Add(i);
                    }
                }
                else
                {
                    string[] flds = fieldsList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach( string f in flds) 
                    {
                        fieldsToProcess.Add(theTable.FieldOrdinal(f));
                    }
                }
                #endregion 

                
                if (outputSwitch && !countOnlySwitch)
                {
                    #region Write records to CSV output file
                    using (var writer = new System.IO.StreamWriter(outputFilePath))
                    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                    {
                        if (whereClauseEvaluatorType == null)
                        {
                            while (!theTable.IsEoT)
                            {
                                recCount++;
                                var rec = new ExpandoObject();
                                for (int i = 0; i < fieldsToProcess.Count; i++)
                                {
                                    rec.TryAdd((string)theTable.FieldName(fieldsToProcess[i]), (object)theTable[fieldsToProcess[i]]);
                                }
                                if (recCount == 1)
                                {
                                    csv.WriteDynamicHeader(rec);
                                    csv.NextRecord();
                                }
                                csv.WriteRecord(rec);
                                csv.NextRecord();
                                theTable.MoveNext();
                            }
                        }
                        else
                        {
                            bool writeHeader = true;
                            while (!theTable.IsEoT)
                            {
                                if ((bool)whereClauseEvaluatorType.GetMethod("Evaluate")!.Invoke(null, new object[] { theTable }))
                                {
                                    recCount++;
                                    var rec = new ExpandoObject();
                                    for (int i = 0; i < fieldsToProcess.Count; i++)
                                    {
                                        rec.TryAdd((string)theTable.FieldName(fieldsToProcess[i]), (object)theTable[fieldsToProcess[i]]);
                                    }
                                    if (writeHeader)
                                    {
                                        csv.WriteDynamicHeader(rec);
                                        csv.NextRecord();
                                        writeHeader = false;
                                    }
                                    csv.WriteRecord(rec);
                                    csv.NextRecord();
                                }
                                theTable.MoveNext();
                                if ((bool)whereClauseEvaluatorType.GetMethod("LowerBoundCheck")!.Invoke(null, new object[] { theTable }))
                                {
                                    break;
                                }
                            }
                        }
                    }
                    #endregion 
                }
                else
                {
                    if(whereClauseEvaluatorType == null)
                    {
                        // If we make it here we have no WHERE clause evaluator, so we will consider all records
                        while (!theTable.IsEoT)
                        {
                            recCount++;
                            if (!countOnlySwitch)
                            {
                                Program.DisplayDataWrite($"{recCount}.\t");
                                for (int i = 0; i < fieldsToProcess.Count; i++)
                                {
                                    //Console.Write($"{theTable.FieldName(fieldsToProcess[i])}: {theTable[fieldsToProcess[i]]}  ");
                                    Console.Write($"{theTable.FieldName(fieldsToProcess[i])}: ");
                                    Program.DisplayDataWrite($"{theTable[fieldsToProcess[i]]}  ");

                                }
                                Console.WriteLine();
                            }
                            theTable.MoveNext();
                        }
                    }
                    else
                    {
                        // If we make it here we have a WHERE clause evaluator, selectively consider records depending on the WHERE clause
                        while (!theTable.IsEoT)
                        {
                            if ((bool)whereClauseEvaluatorType.GetMethod("Evaluate")!.Invoke(null, new object[] { theTable }))
                            {
                                recCount++;
                                if (!countOnlySwitch)
                                {
                                    // If we are not counting only, we will output the record
                                    Program.DisplayDataWrite($"{recCount}.\t");
                                    for (int i = 0; i < fieldsToProcess.Count; i++)
                                    {
                                        Console.Write($"{theTable.FieldName(fieldsToProcess[i])}: ");
                                        Program.DisplayDataWrite($"{theTable[fieldsToProcess[i]]}  ");
                                    }
                                    Console.WriteLine();
                                }
                            }
                            theTable.MoveNext();
                            if((bool)whereClauseEvaluatorType.GetMethod("LowerBoundCheck")!.Invoke(null, new object[] { theTable }))
                            {
                                break;
                            }
                        }
                    }
                    Console.WriteLine();
                }
            }
            return recCount;
        }


        private void ValidateAccessFields(UserSessionInfo userSessionInfo, string[] ContentFileNames, int index)
        {
            if (fieldsList != null!)
            {
                var wcContentFileName = System.IO.Path.Combine("WCOPublishedDataSets", ContentFileNames[index - 1]);

                API.Initialize(new Guid(VirtualDriveSpec.VirtualDiskSessionToken.Substring(1)),
                           new Guid(VirtualDriveSpec.VolumeSessionToken.Substring(1)),
                           userSessionInfo.UserSessionToken);

                using (var theTable = Table.Open(wcContentFileName, (uint)Unosys.SDK.FileAccess.ReadWrite, (uint)Unosys.SDK.FileAttributes.Normal, (uint)Unosys.SDK.FileShare.None))
                {
                    string[] flds = fieldsList.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string field in flds)
                    {
                        bool fieldExists = false;
                        for (int i = 1; i <= theTable.FieldCount; i++)
                        {
                            if (theTable.FieldName(i).Trim().Equals(field, StringComparison.OrdinalIgnoreCase))
                            {
                                fieldExists = true;
                                break;
                            }
                        }
                        if (!fieldExists)
                        {
                            //Program.DisplayUnsuccessfulWriteLine($"Unknown /FIELDS field specified '{field.ToUpper()}'.");
                            throw new CommandLineToolInvalidCommandException($"Unknown /FIELDS field '{field.ToUpper()}' specified.");
                        }
                    }
                }
            }
        }


        private void ValidateWhereClause(UserSessionInfo userSessionInfo, string[] ContentFileNames, int index)
        {

            if (! string.IsNullOrEmpty(whereClause) )
            {
                var wcContentFileName = System.IO.Path.Combine("WCOPublishedDataSets", ContentFileNames[index - 1]);

                API.Initialize(new Guid(VirtualDriveSpec.VirtualDiskSessionToken.Substring(1)),
                           new Guid(VirtualDriveSpec.VolumeSessionToken.Substring(1)),
                           userSessionInfo.UserSessionToken);

                using (var theTable = Table.Open(wcContentFileName, (uint)Unosys.SDK.FileAccess.ReadWrite, (uint)Unosys.SDK.FileAttributes.Normal, (uint)Unosys.SDK.FileShare.None)) 
                {
                    List<string> whereClauseFields = new List<string>();
                    List<string> whereClauseOperators = new List<string>();
                    List<string> whereClauseValues = new List<string>();
                    string[] andClauses = whereClause.ToUpper().Split(new string[] { "AND" }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    foreach (string andClause in andClauses)
                    {
                        var x = andClause.Replace("==", "■").Replace("=", "■").Replace(">■", "■").Replace("<■", "■").Replace("<", "■").Replace(">", "■");  // Split doesn't work well with multiple character seprators, so we replace with a single character separator
                        string[] andClauseParts = x.Split(new string[] { "■" }, StringSplitOptions.TrimEntries);
                        if (andClauseParts.Length != 2  || andClauseParts[1][0] != '\'' || andClauseParts[1][andClauseParts[1].Length - 1] != '\'')
                        {
                            throw new CommandLineToolInvalidCommandException($"Invalid WHERE clause specified {andClause.ToUpper()}.");
                        }
                        bool fieldExists = false;
                        for (int i = 1; i <= theTable.FieldCount; i++)
                        {
                            if (theTable.FieldName(i).Trim().Equals(andClauseParts[0], StringComparison.OrdinalIgnoreCase))
                            {
                                whereClauseFields.Add(andClauseParts[0].Trim().ToUpper());
                                #region Determine clause operator
                                var opBegin = andClause.Substring(andClauseParts[0].Length).Trim();
                                var op = opBegin.Substring(0, opBegin.IndexOf('\'')).Trim();
                                if ( op != "<" && op != "<=" && op != "==" && op != "=" && op != ">=" && op != ">")
                                {
                                    throw new CommandLineToolInvalidCommandException($"Invalid operator '{op}' specified in selection criteria.");
                                }
                                whereClauseOperators.Add(op);
                                #endregion

                                #region Determine clause value
                                var valueBegin = x.Substring( x.IndexOf("■") + 1).Trim(); // Remove the leading and trailing single quotes
                                whereClauseValues.Add(valueBegin.Substring(1, valueBegin.Length - 2));
                                #endregion 
                                fieldExists = true;
                                break;
                            }
                        }
                        if (!fieldExists)
                        {
                            throw new CommandLineToolInvalidCommandException($"Unknown field '{andClauseParts[0].ToUpper()}' specified in selection criteria.");
                        }
                    }

                    #region Generate Where Clause assembly
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("namespace WCSim");
                    sb.AppendLine("{");
                    sb.AppendLine();
                    sb.AppendLine("    public static class WhereClauseEvaluator");
                    sb.AppendLine("    {");
                    sb.AppendLine("        public static bool Evaluate(dynamic t)");
                    sb.AppendLine("        {");
                    string evaluationCondition = "";
                    string lowerBoundCheckCondition = "t.IsEoT"; // default for full table scan
                    string primaryOrder = null!;
                    string primaryOrderOperator = null!;
                    string primaryOrderValue = "";
                    string primaryOrderValue2 = "";
                    string primaryOrderOperator2 = null!;
                    int primaryOrderValueIndex = -1;
                    for (int i = 0; i < whereClauseFields.Count; i++)
                    {
                        if (i > 0)
                        {
                            evaluationCondition += " && ";
                        }
                        if (whereClauseOperators[i] == "==")
                        {
                            evaluationCondition += $"(string.Compare( t.{whereClauseFields[i]},  \"{whereClauseValues[i]}\".PadRight({theTable.FieldLen(whereClauseFields[i])},' ') ) == 0)";
                        }
                        else
                        {
                            evaluationCondition += $"(string.Compare( t.{whereClauseFields[i]}.Substring(0, \"{whereClauseValues[i]}\".Length),  \"{whereClauseValues[i]}\" ) {(whereClauseOperators[i] =="="?"==": whereClauseOperators[i])} 0)";
                        }
                        if (primaryOrder == null && theTable.OrderNameExists(whereClauseFields[i]))
                        {
                            primaryOrder = whereClauseFields[i];
                            primaryOrderOperator = whereClauseOperators[i];
                            primaryOrderValue = whereClauseValues[i];
                            primaryOrderValueIndex = i;
                        }
                    }
                    #region Determine if there is a lower bound check condition
                    List<string> uniqueFields = new List<string>();
                    if (primaryOrderOperator != null)
                    {
                        int primaryOrderCount = 0;
                        for (int i = 0; i < whereClauseFields.Count; i++)
                        {
                            if(!uniqueFields.Contains(whereClauseFields[i]))
                            {
                                uniqueFields.Add(whereClauseFields[i]);
                            }

                            if (whereClauseFields[i].Equals(primaryOrder))
                            {
                                primaryOrderCount++;
                            }
                            if(primaryOrderCount > 1)
                            {
                                primaryOrderOperator2 = whereClauseOperators[i];
                                primaryOrderValue2 = whereClauseValues[i];
                            }
                        }

                        if (uniqueFields.Count == 1)
                        {
                            switch (primaryOrderOperator)
                            {
                                case ">=":
                                    lowerBoundCheckCondition = $"(string.Compare( t.{primaryOrder}.Substring(0, \"{primaryOrderValue2}\".Length),  \"{primaryOrderValue2}\" ) > 0)";
                                    break;
                                case "<=":
                                    lowerBoundCheckCondition = $"(string.Compare( t.{primaryOrder}.Substring(0, \"{primaryOrderValue}\".Length),  \"{primaryOrderValue}\" ) > 0)";
                                    break;
                                case "==":
                                    lowerBoundCheckCondition = $"(string.Compare( t.{primaryOrder},  \"{primaryOrderValue}\".PadRight({theTable.FieldLen(whereClauseFields[primaryOrderValueIndex])},' ') ) != 0)";
                                    break;
                                case "=":
                                    lowerBoundCheckCondition = $"(string.Compare( t.{primaryOrder}.Substring(0, \"{primaryOrderValue}\".Length),  \"{primaryOrderValue}\" ) != 0)";
                                    break;

                            }
                        }
                    }
                    #endregion 
                    sb.AppendLine($"           return ( {evaluationCondition} );");
                    sb.AppendLine("        }");
                    sb.AppendLine();

                    if(primaryOrder != null )
                    {
                        sb.AppendLine($"       public static void SetOrder(dynamic t) {{ t.SetOrder(\"{primaryOrder}\"); }} ");
                    }
                    else
                    {
                        sb.AppendLine($"       public static void SetOrder(dynamic t) {{ t.SetOrder(0); }}");
                    }
                    sb.AppendLine();
                    if(whereClauseFields.Count > 0 && primaryOrder != null && whereClauseFields[0].Equals(primaryOrder) )
                    {
                        if (whereClauseOperators[0] == ">=")
                        {
                            sb.AppendLine($"        public static void PositionToFirstRecord(dynamic t) {{ t.SeekClosest(\"{whereClauseValues[0]}\"); }}");
                        }
                        else if (whereClauseOperators[0] == "==")
                        {
                            sb.AppendLine($"        public static void PositionToFirstRecord(dynamic t) {{ t.SeekExact(\"{whereClauseValues[0]}\".PadRight({theTable.FieldLen(whereClauseFields[0])},' ')); }}");
                        }
                        else if (whereClauseOperators[0] == "=")
                        {
                            sb.AppendLine($"        public static void PositionToFirstRecord(dynamic t) {{ t.SeekPartial(\"{whereClauseValues[0]}\"); }}");
                        }
                        else
                        {
                            sb.AppendLine("        public static void PositionToFirstRecord(dynamic t) { t.GoTop(); }");
                        }
                    }
                    else
                    {
                        sb.AppendLine("        public static void PositionToFirstRecord(dynamic t) { t.GoTop(); }");
                    }
                    sb.AppendLine($"        public static bool LowerBoundCheck(dynamic t) {{ return {lowerBoundCheckCondition}; }}");
                    sb.AppendLine();
                    sb.AppendLine("    }");
                    sb.AppendLine("}");

                    //#region Display generated C# WhereClauseEvaluator selection criteria class code
                    //var sourceCode = sb.ToString();
                    //string[] sourceCodeParts = sourceCode.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                    //int lineCount = 1;
                    //foreach (var s in sourceCodeParts)
                    //{
                    //    Console.WriteLine($"{lineCount++}\t{s}");
                    //}
                    //#endregion 

                    List<MetadataReference> compilerReferences = new List<MetadataReference>();
                    compilerReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
                    compilerReferences.Add(MetadataReference.CreateFromFile("System.Runtime.dll"));
                    compilerReferences.Add(MetadataReference.CreateFromFile("System.Linq.Expressions.dll"));
                    compilerReferences.Add(MetadataReference.CreateFromFile("Microsoft.CSharp.dll"));
                    compilerReferences.Add(MetadataReference.CreateFromFile("System.Console.dll"));


                    (whereClauseAssembly,  CSharpCompilation compilation, SyntaxTree syntaxTree) = AssemblyGenerator.GenerateAssembly(
                        true, OutputKind.DynamicallyLinkedLibrary, false, LanguageVersion.CSharp10,
                        compilerReferences, false, sb.ToString(), "WCSim.WhereClauseEvaluator",null, null, null, null);
                    if (whereClauseAssembly == null)
                    {
                        if (compilation != null)
                            throw new CommandLineToolInvalidCommandException("Compilation of selection critera failed - check that the syntax is correct.");
                    }
                    else
                    {
                        Assembly loadedAssembly = Assembly.Load(whereClauseAssembly);
                        whereClauseEvaluatorType = loadedAssembly.GetType("WCSim.WhereClauseEvaluator");
                        if (whereClauseEvaluatorType == null)
                        {
                            throw new CommandLineToolInvalidCommandException($"Unknown error loading WCSim.WhereClauseEvaluator assembly.");
                        }
                    }

                    #endregion
                }
            }
        }


        private string CallGeneralLedgerPurchaseDataSetOnAPI( string buyerSessionToken, string contentDIDRef, decimal price)
        {
            string result = null!;
            var json = @$"
            {{
                ""V"" : 1,
                ""F"" : 0,
                ""A"" : {/*(int)ApiIdentifier.GeneralLedgerPurchaseDataSet*/756},
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
                                        ""N"" : ""BuyerSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{buyerSessionToken}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""ContentDIDRef"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{contentDIDRef}"",
                            ""N""  : false
                        }},{{
                            ""D"" : {{
                                        ""N"" : ""Price"",
                                        ""T"" : ""System.Decimal"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{price}"",
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
                    //result = $"{amount.ToString("N0")} TBar successfully deposited to GL of {userName.ToUpper()} from Crypto Account: {dltAddress} - Journal Entry Posted:" + Environment.NewLine + Environment.NewLine;
                    result = "";
                    var responseJson = response.Content.ReadAsStringAsync().Result;
                    using var doc = JsonDocument.Parse(responseJson);
                    var element = doc.RootElement;
                    result += element.GetProperty("O")[0].GetProperty("V").GetString()!;
                }
                else
                {
                    string reason = null!;
                    try
                    {
                        reason = Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result);
                    }
                    catch (Exception)
                    {
                        reason = "No reason given.";
                        throw;
                    }
                    Program.DisplaySuccessfulWriteLine($"Error calling CallGeneralLedgerPurchaseDataSetOnAPI() - response.StatusCode={response.StatusCode}{(reason != null ? $" - ({reason})" : "")}");
                    result = null!;
                }
            }
            catch (Exception ex)
            {
                Program.DisplayUnsuccessfulWriteLine($"Error in CallGeneralLedgerPurchaseDataSetOnAPI(): {nodeName} - {ex} ");
            }
            return result;

        }


        private decimal CallGeneralLedgerGetExchangeRateOnAPI()
        {
            decimal result = 0.00000m; ;
            var json = @$"
            {{
                ""V"" : 1,
                ""F"" : 0,
                ""A"" : {/*(int)ApiIdentifier.GeneralLedgerGetExchangeRate*/754},
                ""I"" : [{{
                            ""D"" : {{
                                        ""N"" : ""UserSessionToken"",
                                        ""T"" : ""System.String"",
                                        ""MV"" : false,
                                        ""IN"" : false
                                    }},
                            ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
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
                    result = decimal.Parse(element.GetProperty("O")[0].GetProperty("V").GetString()!);
                }
                else
                {
                    string reason = null!;
                    try
                    {
                        reason = Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result);
                    }
                    catch (Exception)
                    {
                        reason = "No reason given.";
                        throw;
                    }
                    Program.DisplaySuccessfulWriteLine($"Error calling CallGeneralLedgerGetExchangeRateOnAPI() - response.StatusCode={response.StatusCode}{(reason != null ? $" - ({reason})" : "")}");
                }
            }
            catch (Exception ex)
            {
                Program.DisplayUnsuccessfulWriteLine($"Error in CallGeneralLedgerGetExchangeRateOnAPI(): {nodeName} - {ex} ");
            }
            return result;

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
        

        #endregion
    }


}
