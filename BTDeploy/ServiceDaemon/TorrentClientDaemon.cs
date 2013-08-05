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
		protected string ApplicationDataDirectoryPath;

		public TorrentClientDaemon (string applicationDataDirectoryPath)
		{
			ApplicationDataDirectoryPath = applicationDataDirectoryPath;
		}

		public void Start()
		{
			// Make clients.
			var monotTorrentClient = new MonoTorrentClient (ApplicationDataDirectoryPath);
			monotTorrentClient.Start ();

			// Get avaible tcp port.
			var port = SocketHelpers.GetAvailableTCPPort ();

			// Write port to file.
			var portFilePath = Path.Combine (ApplicationDataDirectoryPath, "port");
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