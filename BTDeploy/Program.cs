using System;
using ManyConsole;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Linq;
using BTDeploy.ServiceDaemon;
using BTDeploy.ServiceDaemon.TorrentClients;
using Autofac;
using System.Collections.Generic;
using ServiceStack.ServiceClient.Web;
using System.Net.Sockets;
using System.Net;
using System.IO;
using BTDeploy.Helpers;

namespace BTDeploy
{
	public class Program
	{
		protected static readonly string ServiceDaemonCommand = "service-daemon";

		public static void Main (string[] args)
		{
			// Make application session directory.
			var sessionDirectoryPath = Path.GetTempPath() + Assembly.GetExecutingAssembly().GetName().Name;
			if (!Directory.Exists (sessionDirectoryPath))
				Directory.CreateDirectory (sessionDirectoryPath);

			// Handle if service-daemon command.
			if (args.Any() && args.First () == ServiceDaemonCommand)
			{
				new TorrentClientDaemon (sessionDirectoryPath).Start();
				return;
			}

			// Get port of service-daemon.
			int? port = ServiceDaemonPort (sessionDirectoryPath);
			if (!port.HasValue)
			{
				SpawnServiceDaemon (sessionDirectoryPath);
				do
				{
					port = ServiceDaemonPort (sessionDirectoryPath);
					Thread.Sleep(500);
				}
				while(!port.HasValue);
			}

			// Make container and dispatch command.
			var container = MakeContainer (port.Value);
			var commands = container.Resolve<IEnumerable<ConsoleCommand>> ().Reverse();
			ConsoleCommandDispatcher.DispatchCommand (commands, args, Console.Out);
		}

		protected static IContainer MakeContainer(int port)
		{
			var clientUri = string.Format ("http://localhost:{0}/", port);

			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterAssemblyTypes (typeof(Program).Assembly)
					.Where (t => t.IsAssignableTo<ConsoleCommand> ())
					.As<ConsoleCommand> ()
					.OwnedByLifetimeScope();
			containerBuilder.RegisterType<JsonServiceClient> ()
					.AsImplementedInterfaces()
					.WithParameter (new NamedParameter ("baseUri", clientUri));
			return containerBuilder.Build ();
		}

		protected static int? ServiceDaemonPort(string sessionDirectoryPath)
		{
			// Read port from file.
			var portFilePath = Path.Combine (sessionDirectoryPath, "port");

			// Check if file exists.
			if (!File.Exists (portFilePath))
			{
				return null;
			}

			// Check if port in use (i.e. serivce-daemon is running).
			var port = int.Parse (File.ReadAllText (portFilePath));
			if (SocketHelpers.IsTCPPortAvailable (port))
			{
				return null;
			}
			return port;
		}

		protected static void SpawnServiceDaemon(string sessionDirectoryPath)
		{
			var applicationPath = Assembly.GetExecutingAssembly ().Location;
			var serviceDaemonStartInfo = new ProcessStartInfo();
			serviceDaemonStartInfo.UseShellExecute = false;
			serviceDaemonStartInfo.RedirectStandardOutput = true;
			serviceDaemonStartInfo.RedirectStandardError = true;

			if (Type.GetType ("Mono.Runtime") == null)
			{
				serviceDaemonStartInfo.FileName = applicationPath;
				serviceDaemonStartInfo.Arguments = ServiceDaemonCommand;
			}
			else
			{
				serviceDaemonStartInfo.FileName = "/usr/bin/mono"; // Should probably be resolving this dynamically!!
				serviceDaemonStartInfo.Arguments = string.Format ("{0} {1}", applicationPath, ServiceDaemonCommand);
			}
			Process.Start (serviceDaemonStartInfo);
		}
	}
}