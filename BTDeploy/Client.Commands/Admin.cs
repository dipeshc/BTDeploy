using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BTDeploy.Helpers;
using BTDeploy.ServiceDaemon;
using ManyConsole;
using ServiceStack.Service;
using System.Net;

namespace BTDeploy.Client.Commands
{
	public class Admin : ConsoleCommand
	{
		protected readonly IEnvironmentDetails EnvironmentDetails; 
		protected readonly IRestClient Client;

		public bool Start = false;
		public bool Stop = false;

		public Admin (IEnvironmentDetails environmentDetails, IRestClient client)
		{
			IsCommand ("Admin", "Basic administration commands.");
			HasOption ("start", "Spawns the long lasting background daemon. This process contines downloading and/or seeding after the application has exited.", o => Start = o != null);
			HasOption ("stop", "Terminates the long lasting background daemon, this process would otherwise continuing downloading and/or seeding after the application has exited.", o => Stop = o != null);
			SkipsCommandSummaryBeforeRunning ();

			Client = client;
			EnvironmentDetails = environmentDetails;
		}

		public override int Run (string[] remainingArguments)
		{
			if (Start == Stop)
			{
				if (Start && Stop)
					Console.WriteLine ("Error: Both start and stop cannot be set.");
				else
					Console.WriteLine ("Error: Either start or stop must be set.");
				return 1;
			}

			if (Start)
				SpawnServiceDaemon ();

			if (Stop)
				Client.Delete (new AdminKillRequest ());

			return 0;
		}

		protected void SpawnServiceDaemon()
		{
			var applicationPath = Assembly.GetExecutingAssembly ().Location;
			var serviceDaemonStartInfo = new ProcessStartInfo();
			serviceDaemonStartInfo.UseShellExecute = false;
			serviceDaemonStartInfo.RedirectStandardOutput = true;
			serviceDaemonStartInfo.RedirectStandardError = true;

			if (Type.GetType ("Mono.Runtime") == null)
			{
				serviceDaemonStartInfo.FileName = applicationPath;
				serviceDaemonStartInfo.Arguments = EnvironmentDetails.ServiceDaemonCommand;
			}
			else
			{
				serviceDaemonStartInfo.FileName = "/usr/bin/mono"; // Should probably be resolving this dynamically!!
				serviceDaemonStartInfo.Arguments = string.Format ("{0} {1}", applicationPath, EnvironmentDetails.ServiceDaemonCommand);
			}

			Process.Start (serviceDaemonStartInfo);
		}
	}
}