using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Unosys.SDK
{
	internal static partial class ClientSessionManager
	{
		#region static Field Members
		static internal uint sessionId = 0;
		static internal int basePort = 5000;  // default if not overwritten by configuration
		//static internal int nounce = 1;
		static internal byte[] sessionSymKey = null;
		static internal byte[] sessionSymIV = null;
		static internal byte[] apiToken = new byte[4];  // %TODO%  for now!
		static internal byte[] userToken = null;  // %TODO%  for now!
		static private ClientSocketConnection clientSocketConnection = null;
		static private Random guardGenerator = null;
		#endregion

		#region Constructor
		static ClientSessionManager()
		{
			// Comment out for now!  %LEGACY Unosys SDK%

			//string unoosysProcessBasePort = ConfigurationManager.AppSettings["UNOSYS_PROCESSOR_BASEPORT"];
			//if( !string.IsNullOrEmpty( unoosysProcessBasePort ))
			//{
			//	Int32.TryParse( unoosysProcessBasePort, out basePort );
			//}
			//clientSocketConnection = new ClientSocketConnection( basePort );
			//guardGenerator = new Random();
			//Task t = StartTimer( TimeSpan.FromSeconds( 5 ) );  // %TODO%  Get Frequency of heartbeat from Windows Registry
		}
		#endregion

		#region Helpers
		static private async Task StartTimer( TimeSpan interval )
		{
			
			while (true)
			{
				await Task.Delay( interval );  // Delay one interval period before sending first heartbeat
				await HeartBeatAsync();
			}
		}
		#endregion
	}
}
