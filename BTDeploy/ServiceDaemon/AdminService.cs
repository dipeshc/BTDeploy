using System;
using System.Linq;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using BTDeploy.ServiceDaemon.TorrentClients;
using System.IO;

namespace BTDeploy.ServiceDaemon
{
	[Route("/api/admin/kill", "DELETE")]
	public class AdminKillRequest : IReturnVoid
	{
	}

	public class AdminService : ServiceStack.ServiceInterface.Service
	{
		public void Delete(AdminKillRequest request)
		{
			Response.Close ();
			Environment.Exit (0);
		}
	}
}