using ServiceStack.Service;
using System.IO;
using BTDeploy.Helpers;
using System.Collections.Generic;
using BTDeploy.ServiceDaemon;

namespace BTDeploy.Client.Commands
{
	public class Create : GeneralConsoleCommandBase
	{
		public string Name;
		public string FilesSource;
		public IEnumerable<string> Trackers;
		public bool Add = false;

		public Create (IEnvironmentDetails environmentDetails, IRestClient client) : base(environmentDetails, client, "Creates a new torrent from a file source.")
		{
			HasRequiredOption ("n|name=", "Name of the torrent to be created.", o => Name = o);
			HasRequiredOption ("f|fileSource=", "Source file/s for torrent.", o => FilesSource = o);
			HasOption ("trackers=", "Trackers to add to the torrent. Comma seperation for more than one.", o => Trackers = o.Split (','));
			HasOption ("a|add", "Adds the torrent after it has been created.", o => Add = o != null);
		}

		public override int Run (string[] remainingArguments)
		{
			var filesSourcePath = Path.GetFullPath (FilesSource);
			var torrentFilePath = Path.GetFullPath (Name + ".torrent");

			var outputStream = Client.Post<Stream> ("/api/torrents/create", new TorrentCreateRequest
			{
				Name = Name,
				FilesSource = filesSourcePath,
				Trackers = Trackers
			});

			using (var file = File.OpenWrite(torrentFilePath))
				StreamHelpers.CopyStream (outputStream, file);

			if (Add)
			{
				// If file source is one file, need to adjust the output path to be the current directory.
				string ouputDirectoryPath;
				if (File.Exists (filesSourcePath))
					ouputDirectoryPath = Path.GetDirectoryName (filesSourcePath);
				else
					ouputDirectoryPath = filesSourcePath;

				new Add(EnvironmentDetails, Client)
				{
					OuputDirectoryPath =  ouputDirectoryPath,
					TorrentPath = torrentFilePath,
					Mirror = false,
					Wait = false
				}.Run (new string[] {});
			}

			return 0;
		}
	}
}