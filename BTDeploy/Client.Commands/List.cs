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
	public class List : ClientCommandBase
	{
		public bool IncludeId = false;

		public List (IRestClient client) : base(client)
		{
			IsCommand ("list", "List the current deployments.");
			HasOption ("id", "Include torrent id in output table.", o => IncludeId = o != null);
		}

		public override int Run (string[] remainingArguments)
		{
			var allTorrentDetails = Client.Get (new TorrentsListRequest ());

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

			allTorrentDetails.ToList ().ForEach (torrentDetails =>
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

			Console.Write (table);

			return 0;
		}
	}
}