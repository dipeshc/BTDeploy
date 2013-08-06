using ServiceStack.Service;
using System.IO;
using BTDeploy.Helpers;

namespace BTDeploy.Client.Commands
{
	public class Create : ClientCommandBase
	{
		public string FileSourceDirectory;
		public string TorrentFile;

		public Create (IRestClient client) : base(client, "Adds a torrent to be deployed.")
		{
			HasRequiredOption ("f|FileSourceDirectory=", "Source files for torrent.", o => FileSourceDirectory = o);
			HasRequiredOption ("t|TorrentFile=", "Name of torrent file to be created.", o => TorrentFile = o);
		}

		public override int Run (string[] remainingArguments)
		{
			var fileSourceDirectoryPath = Path.GetFullPath (FileSourceDirectory);
			var torrentFilePath = Path.GetFullPath (TorrentFile);

			var outputStream = Client.Post<Stream> ("/api/torrents/create?FileSourceDirectory=" + fileSourceDirectoryPath, null);

			using (var file = File.OpenWrite(torrentFilePath))
				StreamHelpers.CopyStream (outputStream, file);

			return 0;
		}
	}
}