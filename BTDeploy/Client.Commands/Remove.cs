using ServiceStack.Service;
using System.Linq;
using BTDeploy.Helpers;
using BTDeploy.ServiceDaemon;
using System.Text.RegularExpressions;

namespace BTDeploy.Client.Commands
{
	public class Remove : ClientCommandBase
	{
		public bool Delete = false;

		public Remove (IRestClient client) : base(client)
		{
			IsCommand ("remove", "Removes one or more torrents with matching names/patterns (wildcards supported).");
			HasOption ("d|delete", "Deletes the files along with the torrent.", o => Delete = o != null);
			HasAdditionalArguments (null);
		}

		public override int Run (string[] remainingArguments)
		{
			// Make the patterns.
			var patterns = remainingArguments.Select (p => new Wildcard (p, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToList();

			// Get all the torrents.
			var allTorrentDetails = Client.Get (new TorrentsListRequest ());

			// Do matching.
			var torrentDetailsMatches = allTorrentDetails.Where (torrentDetails => patterns.Any(p => p.Match(torrentDetails.Name).Success)).ToList();

			// Remove each match found.
			torrentDetailsMatches.ForEach (torrentDetails =>
			{
				Client.Delete(new TorrentRemoveRequest
              	{
					Id = torrentDetails.Id,
					DeleteFiles = Delete
				});
			});

			return 0;
		}
	}
}