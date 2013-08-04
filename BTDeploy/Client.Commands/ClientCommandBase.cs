using System;
using ManyConsole;
using ServiceStack.Service;

namespace BTDeploy.Client.Commands
{
	public abstract class ClientCommandBase : ConsoleCommand
	{
		protected IRestClient Client;

		public ClientCommandBase (IRestClient client)
		{
			Client = client;
			SkipsCommandSummaryBeforeRunning ();
		}
	}
}