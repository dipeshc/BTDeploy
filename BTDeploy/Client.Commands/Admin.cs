using ServiceStack.Service;
using System.IO;
using BTDeploy.Helpers;

namespace BTDeploy.Client.Commands
{
	public class Admin : ClientCommandBase
	{
		public Admin (IRestClient client) : base(client, "Basic administration commands.")
		{
		}

		public override int Run (string[] remainingArguments)
		{
			return 0;
		}
	}
}