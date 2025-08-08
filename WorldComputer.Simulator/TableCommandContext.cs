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
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WorldComputer.Simulator
{

    public class TableCommandContext : ICommandContext
    {
        #region Field Members
        const int DEFAULT_CLIENT_NODE_NUMBER = 1;
        const int MAX_TITLE_LENGTH = 200;

        Hashtable switchSet = new Hashtable(20);
        string[] cmdArgs;
        string tableName = null!;
        string dbName = null!;
        string userName = null!;
        bool listSwitch = false;
        bool userSwitch = false;
        bool databaseSwitch = false;
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
        public TableCommandContext(string[] commandContextArgs)
        {
            cmdArgs = commandContextArgs;
            // Define valid context switches
            string[] allowedSwitches = new string[9];
            allowedSwitches[0] = "/CLIENTNODE:";
            allowedSwitches[1] = "/CLOSE:";
            allowedSwitches[2] = "/CREATE:";
            allowedSwitches[3] = "/DB:";
            allowedSwitches[4] = "/DELETE:";
            allowedSwitches[5] = "/LIST:";
            allowedSwitches[6] = "/OPEN:";
            allowedSwitches[7] = "/USER:";
            allowedSwitches[8] = "/?";
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

                #region Create Table
                if (createSwitch)
                {
                    //PublishContent();
                }
                #endregion

                #region Delete Table
                if (deleteSwitch)
                {
                    //AccessContent();
                }
                #endregion

                #region Open Table
                if (openSwitch)
                {
                    //PublishContent();
                }
                #endregion

                #region Close Table
                if (closeSwitch)
                {
                    //AccessContent();
                }
                #endregion

                #region List Table
                if (listSwitch)
                {
                    //ListTables();
                }
                #endregion

            }
        }

        public bool ValidateContext()
        {
            bool invalidSwitch = false;
            object switchValue = null;

            #region Switch Processing
            #region Create Table switch processing
            //Examples of possible legal switch usage: /Create:MyTable
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
                        tableName = values[0];
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

            #region Delete Table switch processing
            //Examples of possible legal switch usage: /Delete:MyTable
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
                        tableName = values[0];
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

            #region Open Table switch processing
            //Examples of possible legal switch usage: /Open:MyTable
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
                        tableName = values[0];
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

            #region Close Table switch processing
            //Examples of possible legal switch usage: /Close:MyTable
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
                        tableName = values[0];
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

            #region List records on table
            //Examples of possible legal switch usage: /List
            switchValue = switchSet["/LIST"];
            if (switchValue is bool)
            {
                listSwitch = (bool)switchValue;
            }
            #endregion


            #region Database switch processing
            switchValue = switchSet["/DB"];
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
                        databaseSwitch = true;
                    }
                    if (invalidSwitch)
                    {
                        throw new CommandLineToolInvalidSwitchException("/DB:" + switchValue);
                    }
                }
            }
            else
            {
                if ((bool)switchValue)
                {
                    throw new CommandLineToolInvalidSwitchException("/DB:" + switchValue);
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
            if (listSwitch && openSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/LIST and /OPEN cannot be used together.");
            }
            if (closeSwitch && listSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/CLOSE and /LIST cannot be used together.");
            }
            if (databaseSwitch && !listSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("/DB cannot be used without /LIST.");
            }
            if (!createSwitch && !simulatorHelp && !openSwitch && !deleteSwitch && !closeSwitch && !listSwitch)
            {
                throw new CommandLineToolInvalidSwitchCombinationException("One of /CREATE or /OPEN or /DELETE or /CLOSE or /LIST is required.");
            }
            #endregion

            bool result = (createSwitch || deleteSwitch || userSwitch || openSwitch || closeSwitch || listSwitch || clientSimulator || simulatorHelp);
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
            Program.DisplaySuccessfulWriteLine("World Computer TABLE command usage:");
            Program.DisplaySuccessfulWriteLine("");
            Program.DisplaySuccessfulWriteLine("      WCSim TABLE | T  [<switches>]");
            Program.DisplaySuccessfulWriteLine("");
            Program.DisplaySuccessfulWriteLine("where <switches> are one or more of:");
            Program.DisplaySuccessfulWriteLine("");
            Program.DisplaySuccessfulWriteLine($" /CLIENTNODE:<Node#>\t\t\tClient Node # to connect to - ({DEFAULT_CLIENT_NODE_NUMBER})");
            Program.DisplaySuccessfulWriteLine( " /CLOSE:<TableName>\t\t\tTable Name to Close");
            Program.DisplaySuccessfulWriteLine( " /CREATE:<TableName>\t\t\tTable Name to Create");
            Program.DisplaySuccessfulWriteLine( " /DELETE:<TableName>\t\t\tTable Name to Delete");
            Program.DisplaySuccessfulWriteLine( " /LIST[:<TableName>]\t\t\tList records in Table (currently opened)");
            Program.DisplaySuccessfulWriteLine( " /OPEN:<TableName>\t\t\tTable Name to Open");
            Program.DisplaySuccessfulWriteLine( " /USER:<UserName>\t\t\tEstablish logged in User context to execute operation in");
            Program.DisplaySuccessfulWriteLine( " /?\t\t\t\t\tUsage information");
            Program.DisplaySuccessfulWriteLine("");
            Program.DisplaySuccessfulWriteLine("=======================================================================================================");
        }
        #endregion


        #region Helpers
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
        //internal static string WCVirtualDiskVolumeDataOperationApi(string base64Operation)
        //{
        //    string result = null!;
        //    var json = @$"
        //    {{
        //        ""V"" : 1,
        //        ""F"" : 0,
        //        ""A"" : {/*(int)ApiIdentifier.VirtualDiskVolumeDataOperation*/ 725},
        //        ""I"" : [{{
        //                    ""D"" : {{
        //                                ""N"" : ""UserSessionToken"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""Operation"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{base64Operation}"",
        //                    ""N""  : false
        //                }}]
        //    }}";

        //    var content = new StringContent(json, Encoding.UTF8, "application/json");
        //    var response = Program.UnoSysApiConnection.PostAsync(content).Result;
        //    if (response.StatusCode == HttpStatusCode.OK)
        //    {
        //        // Retrieve result of call
        //        var responseJson = response.Content.ReadAsStringAsync().Result;
        //        using var doc = JsonDocument.Parse(responseJson);
        //        var element = doc.RootElement;
        //        result = element.GetProperty("O")[0].GetProperty("V").GetString()!;
        //    }
        //    else
        //    {
        //        Program.DisplayUnsuccessfulWriteLine($"Error calling WCVirtualDiskVolumeDataOperationApi() - response.StatusCode={response.StatusCode}");
        //    }
        //    return result;
        //}


        //internal static string WCVirtualDiskVolumeMetaDataOperationApi(string base64Operation)
        //{
        //    string result = null!;
        //    var json = @$"
        //    {{
        //        ""V"" : 1,
        //        ""F"" : 0,
        //        ""A"" : {/*(int)ApiIdentifier.VirtualDiskVolumeMetaDataOperation*/ 724},
        //        ""I"" : [{{
        //                    ""D"" : {{
        //                                ""N"" : ""UserSessionToken"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""Operation"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{base64Operation}"",
        //                    ""N""  : false
        //                }}]
        //    }}";

        //    var content = new StringContent(json, Encoding.UTF8, "application/json");
        //    var response = Program.UnoSysApiConnection.PostAsync(content).Result;
        //    if (response.StatusCode == HttpStatusCode.OK)
        //    {
        //        // Retrieve result of call
        //        var responseJson = response.Content.ReadAsStringAsync().Result;
        //        using var doc = JsonDocument.Parse(responseJson);
        //        var element = doc.RootElement;
        //        result = element.GetProperty("O")[0].GetProperty("V").GetString()!;
        //    }
        //    else
        //    {
        //        Program.DisplayUnsuccessfulWriteLine($"Error calling WCVirtualDiskVolumeMetaDataOperationApi() - response.StatusCode={response.StatusCode}");
        //    }
        //    return result;
        //}
        //private string CallGeneralLedgerGetReportApiOnNode(string userSessionToken, int reportType, int reportOutputType, string fromUtcDate, string toUtcDate )
        //{
        //    string result = null!;
        //    var json = @$"
        //    {{
        //        ""V"" : 1,
        //        ""F"" : 0,
        //        ""A"" : {/*(int)ApiIdentifier.GeneralLedgerGetReport*/750},
        //        ""I"" : [{{
        //                    ""D"" : {{
        //                                ""N"" : ""UserSessionToken"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""SubjectSessionToken"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : true
        //                            }},
        //                    ""V""  : ""{userSessionToken}"",
        //                    ""N""  : true
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""ReportType"",
        //                                ""T"" : ""System.Int32"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{reportType}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""ReportOutputType"",
        //                                ""T"" : ""System.Int32"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{reportOutputType}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""FromUtcDate"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{fromUtcDate}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""ToUtcDate"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{toUtcDate}"",
        //                    ""N""  : false
        //                }}]
        //    }}";

        //    var content = new StringContent(json, Encoding.UTF8, "application/json");
        //    var nodeName = $"{Program.NodeExecutableName} #{clientNodeNumber}";
        //    try
        //    {

        //        var response = Program.UnoSysApiConnection.PostAsync(content).Result;
        //        if (response.StatusCode == HttpStatusCode.OK)
        //        {
        //            // Retrieve result of call
        //            var responseJson = response.Content.ReadAsStringAsync().Result;
        //            using var doc = JsonDocument.Parse(responseJson);
        //            var element = doc.RootElement;
        //            result = element.GetProperty("O")[0].GetProperty("V").GetString()!;
        //        }
        //        else
        //        {
        //            Program.DisplaySuccessfulWriteLine($"Error calling GeneralLedgerGetReport() - response.StatusCode={response.StatusCode}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Program.DisplayUnsuccessfulWriteLine($"Error in CallGeneralLedgerGetReportApiOnNode(): {nodeName} - {ex} ");
        //    }
        //    return result;

        //}


        //private string CallGeneralLedgerFundWalletApiOnNode(string userSessionToken, string dltAddress, string dltPrivateKey, ulong amount)
        //{
        //    string result = null!;
        //    var json = @$"
        //    {{
        //        ""V"" : 1,
        //        ""F"" : 0,
        //        ""A"" : {/*(int)ApiIdentifier.GeneralLedgerFundWallet*/751},
        //        ""I"" : [{{
        //                    ""D"" : {{
        //                                ""N"" : ""UserSessionToken"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""SubjectSessionToken"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : true
        //                            }},
        //                    ""V""  : ""{userSessionToken}"",
        //                    ""N""  : true
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""DLTAddress"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{dltAddress}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""DLTPrivateKey"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{dltPrivateKey}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""FundsAmount"",
        //                                ""T"" : ""System.UInt64"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{amount}"",
        //                    ""N""  : false
        //                }}]
        //    }}";

        //    var content = new StringContent(json, Encoding.UTF8, "application/json");
        //    var nodeName = $"{Program.NodeExecutableName} #{clientNodeNumber}";
        //    try
        //    {
        //        var response = Program.UnoSysApiConnection.PostAsync(content).Result;
        //        if (response.StatusCode == HttpStatusCode.OK)
        //        {
        //            // Retrieve result of call
        //            result = $"{amount.ToString("N0")} TBar successfully deposited to GL of {userName.ToUpper()} from Crypto Account: {dltAddress} - Journal Entry Posted:" + Environment.NewLine + Environment.NewLine;
        //            var responseJson = response.Content.ReadAsStringAsync().Result;
        //            using var doc = JsonDocument.Parse(responseJson);
        //            var element = doc.RootElement;
        //            result += element.GetProperty("O")[0].GetProperty("V").GetString()!;
        //        }
        //        else
        //        {
        //            string reason = null!;
        //            try
        //            {
        //                reason = Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result);
        //            }
        //            catch (Exception)
        //            {
        //                reason = "No reason given.";
        //                throw;
        //            }
        //            Program.DisplaySuccessfulWriteLine($"Error calling GeneralLedgerFundWallet() - response.StatusCode={response.StatusCode}{(reason != null? $" - ({reason})" : "" )}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Program.DisplayUnsuccessfulWriteLine($"Error in CallGeneralLedgerFundWalletApiOnNode(): {nodeName} - {ex} ");
        //    }
        //    return result;

        //}

        //private string CallGeneralLedgerDefundWalletApiOnNode(string userSessionToken, string dltAddress, string dltPrivateKey, ulong amount)
        //{
        //    string result = null!;
        //    var json = @$"
        //    {{
        //        ""V"" : 1,
        //        ""F"" : 0,
        //        ""A"" : {/*(int)ApiIdentifier.GeneralLedgerFundWallet*/752},
        //        ""I"" : [{{
        //                    ""D"" : {{
        //                                ""N"" : ""UserSessionToken"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{Program.WC_OSUSER_SESSION_TOKEN}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""SubjectSessionToken"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : true
        //                            }},
        //                    ""V""  : ""{userSessionToken}"",
        //                    ""N""  : true
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""DLTAddress"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{dltAddress}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""DLTPrivateKey"",
        //                                ""T"" : ""System.String"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{dltPrivateKey}"",
        //                    ""N""  : false
        //                }},{{
        //                    ""D"" : {{
        //                                ""N"" : ""FundsAmount"",
        //                                ""T"" : ""System.UInt64"",
        //                                ""MV"" : false,
        //                                ""IN"" : false
        //                            }},
        //                    ""V""  : ""{amount}"",
        //                    ""N""  : false
        //                }}]
        //    }}";

        //    var content = new StringContent(json, Encoding.UTF8, "application/json");
        //    var nodeName = $"{Program.NodeExecutableName} #{clientNodeNumber}";
        //    try
        //    {

        //        var response = Program.UnoSysApiConnection.PostAsync(content).Result;
        //        if (response.StatusCode == HttpStatusCode.OK)
        //        {
        //            result = $"{(amount == ulong.MaxValue ? "All" : amount.ToString("N0"))} TBar successfully withdrawn from GL of {userName.ToUpper()} to Crypto Account: {dltAddress} - Journal Entry Posted:" + Environment.NewLine + Environment.NewLine;
        //            var responseJson = response.Content.ReadAsStringAsync().Result;
        //            using var doc = JsonDocument.Parse(responseJson);
        //            var element = doc.RootElement;
        //            result += element.GetProperty("O")[0].GetProperty("V").GetString()!;
        //        }
        //        else
        //        {
        //            string reason = null!;
        //            try
        //            {
        //                reason = Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result);
        //            }
        //            catch (Exception)
        //            {
        //                reason = "No reason given.";
        //                throw;
        //            }
        //            Program.DisplaySuccessfulWriteLine($"Error calling GeneralLedgerDefundWallet() - response.StatusCode={response.StatusCode}{(reason != null ? $" - ({reason})" : "")}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Program.DisplayUnsuccessfulWriteLine($"Error in CallGeneralLedgerDefundWalletApiOnNode(): {nodeName} - {ex} ");
        //    }
        //    return result;

        //}

        #endregion
    }


}
