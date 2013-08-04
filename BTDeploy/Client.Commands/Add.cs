using ServiceStack.Service;
using ServiceStack.Common.Web;
using System.IO;
using BTDeploy.ServiceDaemon.TorrentClients;
using BTDeploy.ServiceDaemon;
using System.Linq;
using System.Threading;
using System;

namespace BTDeploy.Client.Commands
{
	public class Add : ClientCommandBase
	{
		public string TorrentPath;
		public string OuputDirectoryPath;
		public bool Mirror = false;
		public bool Wait = false;

		public Add (IRestClient client) : base(client)
		{
			IsCommand ("add", "Adds a torrent to be deployed.");
			HasRequiredOption ("t|torrent=", "Torrent file path.", o => TorrentPath = o);
			HasRequiredOption ("o|outputDirectory=", "Output directory path for downloaded torrent.", o => OuputDirectoryPath = o);
			HasOption ("m|mirror", "Walks the output directory to make sure it mirrors the torrent. Any additional files will be deleted.", o => Mirror = o != null);
			HasOption ("w|wait", "Wait for torrent to finish downloading before exiting.", o => Wait = o != null);
		}

		public override int Run (string[] remainingArguments)
		{
			var postUri = "/api/torrents?OutputDirectoryPath=" + OuputDirectoryPath + "&mirror=" + Mirror.ToString();
			var addedTorrentDetails = Client.PostFile<TorrentDetails> (postUri, new FileInfo(TorrentPath), MimeTypes.GetMimeType (TorrentPath));

			if (!Wait)
				return 0;

			while(true)
			{
				Thread.Sleep(1000);

				var trackedTorrentDetails = Client.Get (new TorrentsListRequest ()).First(torrentDetails => torrentDetails.Id == addedTorrentDetails.Id);

				if (trackedTorrentDetails.Status == TorrentStatus.Error)
					return 1;

				if (trackedTorrentDetails.Status == TorrentStatus.Seeding)
					break;

				var downloadSpeedInKBs = Math.Round(trackedTorrentDetails.DownloadBytesPerSecond / Math.Pow(2, 10), 2);
				Console.Write ("Completed {0:f2}%, {1:f2}KB/s\r", trackedTorrentDetails.Progress, downloadSpeedInKBs);
			}

			return 0;
		}
	}
}