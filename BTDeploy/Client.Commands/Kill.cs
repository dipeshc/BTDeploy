using ManyConsole;
using ServiceStack.Service;
using BTDeploy.ServiceDaemon;

namespace BTDeploy.Client.Commands
{
	public class Kill : ClientCommandBase
	{
		public Kill (IRestClient client) : base(client)
		{
			IsCommand ("kill", "Terminate the service daemon.");
		}

		public override int Run (string[] remainingArguments)
		{
			Client.Delete (new AdminKillRequest ());
			return 0;
		}
	}
}