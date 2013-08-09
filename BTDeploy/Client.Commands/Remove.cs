using ServiceStack.Service;
using System.Linq;
using BTDeploy.Helpers;
using BTDeploy.ServiceDaemon;
using System.Text.RegularExpressions;

namespace BTDeploy.Client.Commands
{
	public class Remove : GeneralConsoleCommandBase
	{
		public bool Delete = false;

		public Remove (IEnvironmentDetails environmentDetails, IRestClient client) : base(environmentDetails, client, "Removes specified deployments. Specify deployments by providing torrent id or wildcard name pattern.")
		{
			HasOption ("d|delete", "Deletes the files along with the torrent deployment file.", o => Delete = o != null);
			HasAdditionalArguments (null);
		}

		public override int Run (string[] remainingArguments)
		{
			// Get all the torrents.
			var allTorrentDetails = Client.Get (new TorrentsListRequest ());

			// Filter.
			var torrentDetailsMatches = FilterByIdOrPattern (remainingArguments, allTorrentDetails);

			// Remove each match found.
			torrentDetailsMatches.ToList().ForEach (torrentDetails =>
			{
				Client.Delete(new TorrentRemoveRequest
              			{
					Id = torrentDetails.Id,
					DeleteFiles = Delete
				});
			});

			if (torrentDetailsMatches.Count () == 0)
				Kill = true;

			return 0;
		}
	}
}