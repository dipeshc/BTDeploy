using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace BTDeploy.Helpers
{
	public static class SocketHelpers
	{
		public static bool IsTCPPortAvailable(int port)
		{
			// The cleaner way of doing this is not supported by mono!! :(
			//return !IPGlobalProperties.GetIPGlobalProperties ().GetActiveTcpConnections ().Any (tcpi => tcpi.LocalEndPoint.Port == port);
			using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
			{
				try
				{
					socket.Connect("localhost", port);
					return false;
				}
				catch (SocketException)
				{
					return true;
				}
			}
		}

		public static int GetAvailableTCPPort()
		{
			var tempListener = new TcpListener(IPAddress.Loopback, 0);
			tempListener.Start();
			var port = ((IPEndPoint)tempListener.LocalEndpoint).Port;
			tempListener.Stop();
			return port;
		}
	}
}