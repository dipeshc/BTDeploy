using ServiceStack.Service;
using ServiceStack.Common.Web;
using System.IO;
using BTDeploy.ServiceDaemon.TorrentClients;
using BTDeploy.ServiceDaemon;
using System.Linq;
using System.Threading;
using System;
using BTDeploy.Helpers;
using System.Text.RegularExpressions;

namespace BTDeploy.Client.Commands
{
	public class Wait : GeneralConsoleCommandBase
	{
		public Wait (IEnvironmentDetails environmentDetails, IRestClient client) : base(environmentDetails, client, "Waits until specified deployments finishes downloading before exiting. Specify deployments by providing torrent id or wildcard name pattern.")
		{
			HasAdditionalArguments (null);
		}

		public override int Run (string[] remainingArguments)
		{
			// If nothing specified we want it all.
			if (!remainingArguments.Any ())
				remainingArguments = new [] { "*" };

			// Get all the torrents.
			var allTorrentDetails = Client.Get (new TorrentsListRequest ()).ToList ();

			// Filter.
			var torrentDetailsMatchIds = FilterByIdOrPattern (remainingArguments, allTorrentDetails)
											.Select(td => td.Id).ToList();

			while(true)
			{
				Thread.Sleep(1000);

				var trackedTorrentDetails = Client.Get (new TorrentsListRequest ())
													.Where(torrentDetails => torrentDetailsMatchIds.Contains(torrentDetails.Id))
													.ToList();
				var trackedTorrentDetailsCount = trackedTorrentDetails.Count ();
				var completedCount = trackedTorrentDetails.Count (td => td.Status != TorrentStatus.Hashing && td.Status != TorrentStatus.Downloading);

				if (trackedTorrentDetailsCount == 0)
					break;

				var progress = trackedTorrentDetails.Average (td => td.Progress);
				var downloadSpeedInKBs = Math.Round(trackedTorrentDetails.Sum(td => td.DownloadBytesPerSecond) / Math.Pow(2, 10), 2);

				Console.Write ("Completed {0}/{1}, {2:f2}%, {3:f2}KB/s\r", completedCount, trackedTorrentDetailsCount, progress, downloadSpeedInKBs);

				if(completedCount == trackedTorrentDetailsCount)
					break;
			}

			return 0;
		}
	}
}