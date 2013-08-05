using System;
using System.Linq;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using BTDeploy.ServiceDaemon.TorrentClients;
using System.IO;
using BTDeploy.Helpers;

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
			// Get the uploaded torrent file and save it temporarily.
			var uploadedTorrentFile = base.RequestContext.Files.First ();
			var tempTorrentFilePath = Path.Combine (Path.GetTempPath (), Path.GetTempFileName());
			uploadedTorrentFile.SaveTo (tempTorrentFilePath);

			// Add the torrent and ge the torrent details.
			var addedTorrentId = TorrentClient.Add (tempTorrentFilePath, request.OutputDirectoryPath);
			var addedTorrentDetails = TorrentClient.List ().First (t => t.Id == addedTorrentId);

			// Remove the temporarily file.
			File.Delete (tempTorrentFilePath);

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

				FileSystem.DeleteEmptyDirectory (addedTorrentDetails.OutputDirectory);
			}
			
			// Return the added torrent details.
			return addedTorrentDetails;
		}

		public void Delete(TorrentRemoveRequest request)
		{
			TorrentClient.Remove (request.Id, request.DeleteFiles);
		}
	}
}