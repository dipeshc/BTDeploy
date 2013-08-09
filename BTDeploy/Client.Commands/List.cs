using System;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using BTDeploy.ServiceDaemon;
using System.Linq;
using BTDeploy.Helpers;
using System.Collections.Generic;

namespace BTDeploy.Client.Commands
{
	public class List : GeneralConsoleCommandBase
	{
		public bool IncludeId = false;

		public List (IEnvironmentDetails environmentDetails, IRestClient client) : base(environmentDetails, client, "Lists the current deployments. Filter deployments by providing torrent id or wildcard name pattern.")
		{
			HasOption ("id", "Include torrent id in output table.", o => IncludeId = o != null);
			HasAdditionalArguments (null);
		}

		public override int Run (string[] remainingArguments)
		{
			// Get all the torrents.
			var allTorrentDetails = Client.Get (new TorrentsListRequest ());

			// If nothing specified we want it all.
			if (!remainingArguments.Any ())
				remainingArguments = new [] { "*" };

			// Filter.
			var torrentDetailsMatches = FilterByIdOrPattern (remainingArguments, allTorrentDetails);


			var headers = new List<string>
			{
				"Name",
				"Status",
				"Output",
				"Size (MiB)",
				"Progress (%)",
				"Down (KB/s)",
				"Up (KB/s)"
			};

			if (IncludeId) headers.Insert (0, "Id");

			var table = new ConsoleTable (headers.ToArray());

			torrentDetailsMatches.ToList ().ForEach (torrentDetails =>
			{
				var sizeInMegaBytes = Math.Round(torrentDetails.Size / Math.Pow(2, 20), 2);
				var progress = Math.Round(torrentDetails.Progress, 2);
				var downloadSpeedInKBs = Math.Round(torrentDetails.DownloadBytesPerSecond / Math.Pow(2, 10), 2);
				var uploadSpeedInKBs = Math.Round(torrentDetails.UploadBytesPerSecond / Math.Pow(2, 10), 2);

				var row = new List<string>
				{
					torrentDetails.Name,
					torrentDetails.Status.ToString(),
					torrentDetails.OutputDirectory,
					sizeInMegaBytes.ToString(),
					progress.ToString(),
					downloadSpeedInKBs.ToString(),
					uploadSpeedInKBs.ToString()
				};

				if (IncludeId) row.Insert (0, torrentDetails.Id);

				table.AddRow(row.ToArray());
			});

			if (torrentDetailsMatches.Count () == 0)
				Kill = true;

			Console.Write (table);

			return 0;
		}
	}
}