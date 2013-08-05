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
	public class Wait : ClientCommandBase
	{
		public Wait (IRestClient client) : base(client, "Wait for one or more deployments to finish downloading before exiting.")
		{
			HasAdditionalArguments (null);
		}

		public override int Run (string[] remainingArguments)
		{
			// Set ids and patterns.
			var ids = remainingArguments;
			var patterns = remainingArguments.Select (p => new Wildcard (p, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToList();

			// Get all the torrents.
			var allTorrentDetails = Client.Get (new TorrentsListRequest ());

			// Do matching.
			var torrentDetailsIdMatches = allTorrentDetails.Where (torrentDetails => ids.Contains (torrentDetails.Id));
			var torrentDetailsPatternMatches = allTorrentDetails.Where (torrentDetails => patterns.Any(p => p.Match(torrentDetails.Name).Success)).ToList();
			var torrentDetailsMatchesId = Enumerable.Union (torrentDetailsIdMatches, torrentDetailsPatternMatches).Select (td => td.Id).ToList ();

			while(true)
			{
				Thread.Sleep(1000);

				var trackedTorrentDetails = Client.Get (new TorrentsListRequest ())
													.Where(torrentDetails => torrentDetailsMatchesId.Contains(torrentDetails.Id))
													.ToList();

				var inProgressCount = trackedTorrentDetails.Count (td => td.Status == TorrentStatus.Hashing || td.Status == TorrentStatus.Downloading);
				if (inProgressCount == 0)
					break;

				var progress = trackedTorrentDetails.Average (td => td.Progress);
				var downloadSpeedInKBs = Math.Round(trackedTorrentDetails.Sum(td => td.DownloadBytesPerSecond) / Math.Pow(2, 10), 2);

				Console.Write ("Completed {0}/{1}, {2:f2}%, {3:f2}KB/s\r", inProgressCount, trackedTorrentDetails.Count (), progress, downloadSpeedInKBs);
			}

			return 0;
		}
	}
}