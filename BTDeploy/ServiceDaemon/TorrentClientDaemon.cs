using System;
using BTDeploy.ServiceDaemon.TorrentClients;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using BTDeploy.Helpers;

namespace BTDeploy.ServiceDaemon
{
	public class TorrentClientDaemon
	{
		protected string SessionDirectoryPath;

		public TorrentClientDaemon (string sessionDirectoryPath)
		{
			SessionDirectoryPath = sessionDirectoryPath;
		}

		public void Start()
		{
			// Make clients.
			var monotTorrentClient = new MonoTorrentClient (SessionDirectoryPath);
			monotTorrentClient.Start ();

			// Get avaible tcp port.
			var port = SocketHelpers.GetAvailableTCPPort ();

			// Write port to file.
			var portFilePath = Path.Combine (SessionDirectoryPath, "port");
			File.WriteAllText (portFilePath, port.ToString ());

			// Make service.
			var serivceHost = new TorrentClientAppHost (monotTorrentClient);
			serivceHost.Init();
			serivceHost.Start(string.Format("http://*:{0}/", port));

			// Never die.
			Thread.Sleep (Timeout.Infinite);
		}
	}
}