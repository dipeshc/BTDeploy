using System;
using System.IO;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using BTDeploy.ServiceDaemon;
using System.Linq;
using BTDeploy.Helpers;

namespace BTDeploy.Client.Commands
{
	public class List : ClientCommandBase
	{
		public string TorrentPath;
		public string OuputDirectoryPath;
		public bool Wait = false;

		public List (IRestClient client) : base(client)
		{
			IsCommand ("list", "List the current deployments.");
		}

		public override int Run (string[] remainingArguments)
		{
			var allTorrentDetails = Client.Get (new TorrentsListRequest ());

			var table = new ConsoleTable (
								"Name",
			                    "Status",
								"Output",
			                    "Size (MiB)",
			                    "Progress (%)",
			                    "Down (KB/s)",
			                    "Up (KB/s)"
			                    );

			allTorrentDetails.ToList ().ForEach (torrentDetails =>
			{
				var sizeInMegaBytes = Math.Round(torrentDetails.Size / Math.Pow(2, 20), 2);
				var progress = Math.Round(torrentDetails.Progress, 2);
				var downloadSpeedInKBs = Math.Round(torrentDetails.DownloadBytesPerSecond / Math.Pow(2, 10), 2);
				var uploadSpeedInKBs = Math.Round(torrentDetails.UploadBytesPerSecond / Math.Pow(2, 10), 2);

				table.AddRow(torrentDetails.Name,
				                    torrentDetails.Status.ToString(),
				             		torrentDetails.OutputDirectory,
				                    sizeInMegaBytes.ToString(),
				                    progress.ToString(),
				                    downloadSpeedInKBs.ToString(),
				                    uploadSpeedInKBs.ToString()
				                    );
			});

			Console.Write (table);

			return 0;
		}
	}
}