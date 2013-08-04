using System;
using ManyConsole;
using ServiceStack.Service;
using BTDeploy.ServiceDaemon;

namespace BTDeploy.Client.Commands
{
	public abstract class ClientCommandBase : ConsoleCommand
	{
		protected IRestClient Client;

		protected bool Kill = false;

		public ClientCommandBase (IRestClient client)
		{
			Client = client;

			HasOption ("k|kill", "Terminates the long lasting background daemon, this process would otherwise continuing downloading and/or seeding after the application has exited.", o => Kill = o != null);

			SkipsCommandSummaryBeforeRunning ();

			AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
			{
				if (Kill) Client.Delete (new AdminKillRequest ());
			};
		}
	}
}