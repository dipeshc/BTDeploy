using System;
using ManyConsole;
using ServiceStack.Service;
using BTDeploy.ServiceDaemon;
using System.Collections.Generic;
using BTDeploy.ServiceDaemon.TorrentClients;
using BTDeploy.Helpers;
using System.Text.RegularExpressions;
using System.Linq;

namespace BTDeploy.Client.Commands
{
	public abstract class ClientCommandBase : ConsoleCommand
	{
		protected IRestClient Client;

		protected bool Kill = false;

		public ClientCommandBase (IRestClient client, string oneLineDescription)
		{
			Client = client;

			IsCommand (this.GetType ().Name, oneLineDescription);

			HasOption ("k|kill", "Terminates the long lasting background daemon, this process would otherwise continuing downloading and/or seeding after the application has exited.", o => Kill = o != null);

			SkipsCommandSummaryBeforeRunning ();

			AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
			{
				if (Kill) Client.Delete (new AdminKillRequest ());
			};
		}

		protected IEnumerable<ITorrentDetails> FilterByIdOrPattern(IEnumerable<string> idOrPatterns, IEnumerable<ITorrentDetails> torrentDetailsCollection)
		{
			// Set ids and patterns.
			var ids = idOrPatterns;
			var patterns = idOrPatterns.Select (p => new Wildcard (p, RegexOptions.IgnoreCase | RegexOptions.Compiled)).ToList();

			// Do matching.
			var torrentDetailsIdMatches = torrentDetailsCollection.Where (torrentDetails => ids.Contains (torrentDetails.Id));
			var torrentDetailsPatternMatches = torrentDetailsCollection.Where (torrentDetails => patterns.Any(p => p.Match(torrentDetails.Name).Success)).ToList();

			// Return filtered list.
			return Enumerable.Union (torrentDetailsIdMatches, torrentDetailsPatternMatches).ToList ();
		}
	}
}