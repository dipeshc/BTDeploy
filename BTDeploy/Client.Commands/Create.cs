using ServiceStack.Service;
using System.IO;
using BTDeploy.Helpers;
using System.Collections.Generic;
using BTDeploy.ServiceDaemon;

namespace BTDeploy.Client.Commands
{
	public class Create : ClientCommandBase
	{
		public string FileSourceDirectory;
		public string TorrentFile;
		public IEnumerable<string> Trackers;
		public bool Add = false;

		public Create (IRestClient client) : base(client, "Adds a torrent to be deployed.")
		{
			HasRequiredOption ("f|fileSourceDirectory=", "Source files for torrent.", o => FileSourceDirectory = o);
			HasRequiredOption ("t|torrentFile=", "Name of torrent file to be created.", o => TorrentFile = o);
			HasOption ("trackers=", "Trackers to add to the torrent. Comma seperation for more than one.", o => Trackers = o.Split (','));
			HasOption ("a|add", "Adds the torrent after it has been created.", o => Add = o != null);
		}

		public override int Run (string[] remainingArguments)
		{
			var fileSourceDirectoryPath = Path.GetFullPath (FileSourceDirectory);
			var torrentFilePath = Path.GetFullPath (TorrentFile);

			var outputStream = Client.Post<Stream> ("/api/torrents/create", new TorrentCreateRequest
			{
				FileSourceDirectory = fileSourceDirectoryPath,
				Trackers = Trackers
			});

			using (var file = File.OpenWrite(torrentFilePath))
				StreamHelpers.CopyStream (outputStream, file);

			if (Add)
			{
				new Add(Client)
				{
					OuputDirectoryPath =  fileSourceDirectoryPath,
					TorrentPath = torrentFilePath,
					Mirror = false,
					Wait = false
				}.Run (new string[] {});
			}

			return 0;
		}
	}
}