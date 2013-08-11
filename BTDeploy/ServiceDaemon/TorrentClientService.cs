using System;
using System.Linq;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using BTDeploy.ServiceDaemon.TorrentClients;
using System.IO;
using BTDeploy.Helpers;
using System.Collections.Generic;
using ServiceStack.Common.Web;

namespace BTDeploy.ServiceDaemon
{
	[Route("/api/torrents", "GET")]
	public class TorrentsListRequest : IReturn<ITorrentDetails[]>
	{
	}

	[Route("/api/torrents", "POST")]
	public class TorrentAddRequest : IReturn<ITorrentDetails>
	{
		public string OutputDirectoryPath { get; set; }
		public bool Mirror { get; set; }
	}

	[Route("/api/torrents/{Id}", "DELETE")]
	public class TorrentRemoveRequest : IReturnVoid
	{
		public string Id { get; set; }
		public bool DeleteFiles { get; set; }
	}

	[Route("/api/torrents/create", "POST")]
	public class TorrentCreateRequest : IReturn
	{
		public string Name { get; set; }
		public string FilesSource { get; set; }
		public IEnumerable<string> Trackers { get; set; }
	}

	public class TorrentClientService : ServiceStack.ServiceInterface.Service
	{
		protected readonly ITorrentClient TorrentClient;

		public TorrentClientService(ITorrentClient torrentClient)
		{
			TorrentClient = torrentClient;
		}

		public ITorrentDetails[] Get(TorrentsListRequest request)
		{
			return TorrentClient.List ().ToArray();
		}

		public ITorrentDetails Post(TorrentAddRequest request)
		{
			// Get the uploaded torrent file.
			var uploadedTorrentFile = base.RequestContext.Files.First ();
			var outputDirectoryPath = request.OutputDirectoryPath;

			// Check output directory isn't a file.
			if (File.Exists (outputDirectoryPath))
				throw HttpError.Conflict ("OutputDirectoryPath already exists as a file. OutputDirectoryPath must be a directory or not exist.");

			// Add the torrent and get the torrent details.
			var addedTorrentId = TorrentClient.Add (uploadedTorrentFile.InputStream, request.OutputDirectoryPath);
			var addedTorrentDetails = TorrentClient.List ().First (t => t.Id == addedTorrentId);

			// Mirror.
			if (request.Mirror)
			{
				Directory.GetFiles (addedTorrentDetails.OutputDirectory, "*", SearchOption.AllDirectories).ToList ().ForEach (currentFile =>
				{
					var relativeFilePath = currentFile.Replace(addedTorrentDetails.OutputDirectory, "")
						.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

					if (!addedTorrentDetails.Files.Contains (relativeFilePath))
						File.Delete(currentFile);
				});

				FileSystemHelpers.DeleteEmptyDirectory (addedTorrentDetails.OutputDirectory);
			}
			
			// Return the added torrent details.
			return addedTorrentDetails;
		}

		public void Delete(TorrentRemoveRequest request)
		{
			TorrentClient.Remove (request.Id, request.DeleteFiles);
		}

		public Stream Post(TorrentCreateRequest request)
		{
			return TorrentClient.Create (request.Name, request.FilesSource, request.Trackers);
		}
	}
}