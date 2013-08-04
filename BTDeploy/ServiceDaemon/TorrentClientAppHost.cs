using ServiceStack.WebHost.Endpoints;
using Funq;
using System.Reflection;
using ServiceStack.Service;
using BTDeploy.ServiceDaemon.TorrentClients;

namespace BTDeploy.ServiceDaemon
{
	public class TorrentClientAppHost : AppHostHttpListenerBase
	{
		protected ITorrentClient TorrentClient;

		public TorrentClientAppHost(ITorrentClient torrentClient) : base(Assembly.GetExecutingAssembly().FullName, typeof (TorrentClientAppHost).Assembly)
		{
			TorrentClient = torrentClient;
		}

		public override void Configure (Container container)
		{
			container.Register<ITorrentClient> (TorrentClient);
		}
	}
}