using ServiceStack.WebHost.Endpoints;
using Funq;
using System.Reflection;
using ServiceStack.Service;
using BTDeploy.ServiceDaemon.TorrentClients;
using System.IO;
using BTDeploy.Helpers;

namespace BTDeploy.ServiceDaemon
{
	public class ServicesAppHost : AppHostHttpListenerBase
	{
		protected readonly ITorrentClient TorrentClient;
		protected readonly string Endpoint;

		public ServicesAppHost(string applicationDataDirectoryPath, ITorrentClient torrentClient) : base(Assembly.GetExecutingAssembly().FullName, typeof (ServicesAppHost).Assembly)
		{
			// Set the torrent client.
			TorrentClient = torrentClient;

			// Ask OS for port and make the endpoint.
			var port = SocketHelpers.GetAvailableTCPPort ();
			Endpoint = string.Format ("http://*:{0}/", port);

			// Save the port.
			var portFilePath = Path.Combine (applicationDataDirectoryPath, "port");
			File.WriteAllText (portFilePath, port.ToString ());
		}

		public override void Configure (Container container)
		{
			// Register the torrent client to container for injection.
			container.Register<ITorrentClient> (TorrentClient);

			// Start.
			Start (Endpoint);
		}
	}
}