using ServiceStack.Service;
using ServiceStack.Common.Web;
using System.IO;
using BTDeploy.ServiceDaemon.TorrentClients;
using BTDeploy.ServiceDaemon;
using System.Linq;
using System.Threading;
using System;
using ServiceStack.ServiceClient.Web;

namespace BTDeploy.Client.Commands
{
	public class Add : GeneralConsoleCommandBase
	{
		public string TorrentPath;
		public string OuputDirectoryPath;
		public bool Mirror = false;
		public bool Wait = false;

		public Add (IEnvironmentDetails environmentDetails, IRestClient client) : base(environmentDetails, client, "Adds a torrent to be deployed.")
		{
			HasRequiredOption ("t|torrent=", "Torrent deployment file path.", o => TorrentPath = o);
			HasRequiredOption ("o|outputDirectory=", "Output directory path for deployment.", o => OuputDirectoryPath = o);
			HasOption ("m|mirror", "Walks the output directory to make sure it mirrors the deployment. Any additional files will be deleted.", o => Mirror = o != null);
			HasOption ("w|wait", "Wait for deployment to finish downloading before exiting.", o => Wait = o != null);
		}

		public override int Run (string[] remainingArguments)
		{
			var OutputDirectoryPathFull = Path.GetFullPath (OuputDirectoryPath);
			var postUri = "/api/torrents?OutputDirectoryPath=" + OutputDirectoryPathFull + "&mirror=" + Mirror.ToString();

			var addedTorrentDetails = Client.PostFile<ITorrentDetails> (postUri, new FileInfo(TorrentPath), MimeTypes.GetMimeType (TorrentPath));

			if (!Wait)
				return 0;

			var waitCommand = new Wait (EnvironmentDetails, Client);
			return waitCommand.Run (new [] { addedTorrentDetails.Id });
		}
	}
}