﻿using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace WorldComputer.Simulator
{
	class Program
	{
        private static IConfigurationRoot HostConfig = null;
        internal static string WC_OSUSER_SESSION_TOKEN = "U00000000000000000000000000000000";
        public static int BasePort = -1;
        public static int BasePortSpacing = -1;
        public static string WorkingDir = null!;
        public static string NodeDirectory = null!;
        public static string NodeExecutableName = null!;
        public static string LocalStoreDirectoryName = null!;
        public static string WebBrowserPath = null!;
        public static int MediaPlayerServerPort = 0;
        public static string MediaPlayerAppName = null!;
        public static int MinimumLoggingLevel = -1;
        public static bool InvalidSwitch = false;
        internal static UnoSysConnection UnoSysApiConnection = null!;
        internal static string UnoSysApiUrlTemplate = "https://localhost:{0}/";
        static void Main( string[] args )
		{

            //WorkingDir = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            var c = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appSettings.json"), optional: false);
            HostConfig = c.Build();
            if (!int.TryParse(HostConfig["WCSimConfig:MIN_LOG_LEVEL"], out MinimumLoggingLevel))
            {
                throw new ArgumentException($"Invalid MIN_LOG_LEVEL configuration.");
            }
            if( MinimumLoggingLevel < 0 ) { MinimumLoggingLevel = 0; }
            if (MinimumLoggingLevel > 6) { MinimumLoggingLevel = 6; }


            if (!int.TryParse(HostConfig["WCSimConfig:BASE_NODE_PORT"], out BasePort))
            {
                throw new ArgumentException($"Invalid BASE_NODE_PORT configuration.");
            }

            if (!int.TryParse(HostConfig["WCSimConfig:BASE_PORT_SPACING"], out BasePortSpacing))
            {
                throw new ArgumentException($"Invalid BASE_PORT_SPACING configuration.");
            }

            NodeDirectory = HostConfig["WCSimConfig:NODE_DIRECTORY"];
            if( string.IsNullOrEmpty( NodeDirectory ) )
            {
                throw new ArgumentException($"NODE_DIRECTORY configuration cannot be empty.");
            }
            WorkingDir = NodeDirectory;

            NodeExecutableName = HostConfig["WCSimConfig:NODE_EXECUTABLE_NAME"];
            if (string.IsNullOrEmpty(NodeDirectory))
            {
                throw new ArgumentException($"NODE_EXECUTABLE_NAME configuration cannot be empty.");
            }

            LocalStoreDirectoryName = HostConfig["WCSimConfig:NODE_LOCALSTORE_DIRECTORY_NAME"];
            if (string.IsNullOrEmpty(LocalStoreDirectoryName))
            {
                throw new ArgumentException($"NODE_LOCALSTORE_DIRECTORY_NAME configuration cannot be empty.");
            }
            WebBrowserPath = HostConfig["WCSimConfig:WEB_BROWSER_PATH"];
            if (string.IsNullOrEmpty(WebBrowserPath))
            {
                throw new ArgumentException($"WEB_BROWSER_PATH configuration cannot be empty.");
            }
            MediaPlayerAppName = HostConfig["WCSimConfig:MEDIA_PLAYER_APPNAME"];
            if (string.IsNullOrEmpty(MediaPlayerAppName))
            {
                throw new ArgumentException($"MEDIA_PLAYER_APPNAME configuration cannot be empty.");
            }
            if( !int.TryParse(HostConfig["WCSimConfig:MEDIA_PLAYER_SERVER_PORT"], out MediaPlayerServerPort))
            {
                throw new ArgumentException($"MEDIA_PLAYER_SERVER_PORT configuration invalid or empty.");
            }
            if (!ProcessorExists())
            {
                throw new CommandLineToolInvalidOperationException($"Cannot locate NODE_EXECUTABLE_NAME {Program.NodeExecutableName}.exe in  NODE_DIRECTORY {Program.NodeDirectory}.  Check configuration.");
            }


            #region Ignore
            Simulator wcSim = null;
            try
            {
                // Process input
                wcSim = new Simulator();
                wcSim.ProcessCommand(args);

            }
            catch (CommandLineToolInvalidSwitchCombinationException e)
            {
                DisplayUnsuccessfulWriteLine("INVALID SWITCH COMBINATION: " + e.Message);
            }
            catch (CommandLineToolDuplicateSwitchesException e)
            {
                DisplayUnsuccessfulWriteLine("INVALID DUPLICATE SWITCH: " + e.Message);
            }

            catch (CommandLineToolInvalidOperationException e)
            {
                DisplayUnsuccessfulWriteLine("INVALID OPERATION: " + e.Message);
            }
            catch (CommandLineToolInvalidSwitchException e)
            {
                DisplayUnsuccessfulWriteLine("INVALID SWITCH: " + e.Message);
            }
            catch (CommandLineToolInvalidCommandException e)
            {
                DisplayUnsuccessfulWriteLine("INVALID COMMAND: " + e.Message);
            }
            catch (CommandLineToolInvalidSwitchArgumentException e)
            {
                DisplayUnsuccessfulWriteLine("INVALID PARAMETER: " + e.Message);
            }
            catch (Exception e)
            {
                DisplayUnsuccessfulWriteLine($"UNEXPECTED ERROR: {e}");
            }

            finally
            {
                if (wcSim != null)
                    wcSim.Dispose();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine();
            }
            #endregion 
            if (!InvalidSwitch)
            {
                DisplaySuccessfulWriteLine("Done");
            }
		}

        static internal void DisplayUnsuccessfulWriteLine(string message)
        {
            var orgColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            //Console.Error.WriteLine($"");
            Console.Error.WriteLine(message);
            Console.ForegroundColor = orgColor;
        }

        static internal void DisplaySuccessfulWriteLine(string message)
        {
            var orgColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            //Console.Error.WriteLine($"");
            Console.Error.WriteLine(message);
            Console.ForegroundColor = orgColor;
        }

        static internal void DisplaySuccessfulWrite(string message)
        {
            var orgColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Error.Write(message);
            Console.ForegroundColor = orgColor;
        }

        static internal void DisplayDataWrite(string message)
        {
            var orgColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Error.Write(message);
            Console.ForegroundColor = orgColor;
        }

        private static bool ProcessorExists()
        {
            return File.Exists(Path.Combine(Program.NodeDirectory, Program.NodeExecutableName + ".exe"));
        }

        internal static int ComputeNodeNumberFromPort(int port)
        {
            return (((port - 1) - BasePort) / BasePortSpacing) + 1;
        }

        internal static string ComputeNodeEndPontFromNodeNumber( int nodeNumber)
        {
            var port = BasePort + ((nodeNumber - 1) * BasePortSpacing);
            return $"https:\\\\localhost:{port}\\";
        }


        //import in the declaration for GenerateConsoleCtrlEvent
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GenerateConsoleCtrlEvent(ConsoleCtrlEvent sigevent, int dwProcessGroupId);
        public enum ConsoleCtrlEvent
        {
            CTRL_C = 0,
            CTRL_BREAK = 1,
            CTRL_CLOSE = 2,
            CTRL_LOGOFF = 5,
            CTRL_SHUTDOWN = 6
        }

        //set up the parents CtrlC event handler, so we can ignore the event while sending to the child
        public static volatile bool SENDING_CTRL_C_TO_CHILD = false;
        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = SENDING_CTRL_C_TO_CHILD;
        }

      

        static void RunWCNode(string pid)
        {
            //var startup = new STARTUPINFO();
            //startup.cb = Marshal.SizeOf<STARTUPINFO>();
            //if (!CreateProcess(null, @"C:\WorldComputer\WorldComputer\e193d2e7adf56b7a0b19ff26b154a6db\win-x64\WCNode.exe" + arguments, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, @"C:\WorldComputer\WorldComputer\e193d2e7adf56b7a0b19ff26b154a6db\win-x64\", ref startup, out var info))
            //    throw new Win32Exception(Marshal.GetLastWin32Error());

            //CloseHandle(info.hProcess);
            //CloseHandle(info.hThread);

            //var process = Process.GetProcessById(info.dwProcessId);


            var process = Process.GetProcessById(int.Parse(pid));
            Console.CancelKeyPress += (s, e) =>
            {
                process.WaitForExit();
                Console.WriteLine("Abort.");
                // end of program is here
            };

            process.WaitForExit();
            Console.WriteLine("Exit.");
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [DllImport("kernel32")]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcess(
           string lpApplicationName,
           string lpCommandLine,
           IntPtr lpProcessAttributes,
           IntPtr lpThreadAttributes,
           bool bInheritHandles,
           int dwCreationFlags,
           IntPtr lpEnvironment,
           string lpCurrentDirectory,
           ref STARTUPINFO lpStartupInfo,
           out PROCESS_INFORMATION lpProcessInformation);
    }
}
