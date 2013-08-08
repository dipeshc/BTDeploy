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
			// Make application data directory.
			var applicationDataDirectoryPath = ApplicationDataDirectoryPath ();

			// Handle if service-daemon command.
			if (args.Any() && args.First () == ServiceDaemonCommand)
			{
				// Make the torrent client.
				var monotTorrentClient = new MonoTorrentClient (applicationDataDirectoryPath);
				monotTorrentClient.Start ();

				// Make torrent service app host.
				var servicesAppHost = new ServicesAppHost (applicationDataDirectoryPath, monotTorrentClient);
				servicesAppHost.Init ();

				// Never die.
				Thread.Sleep (Timeout.Infinite);
				return;
			}

			// Get port of service-daemon.
			int? port = ServiceDaemonPort (applicationDataDirectoryPath);
			if (!port.HasValue)
			{
				SpawnServiceDaemon (applicationDataDirectoryPath);
				do
				{
					port = ServiceDaemonPort (applicationDataDirectoryPath);
					Thread.Sleep(500);
				}
				while(!port.HasValue);
			}

			// Make container and dispatch command.
			var container = MakeContainer (port.Value);
			var commands = container.Resolve<IEnumerable<ConsoleCommand>> ().OrderBy (c => c.Command);
			ConsoleCommandDispatcher.DispatchCommand (commands, args, Console.Out);
		}

		protected static string ApplicationDataDirectoryPath()
		{
			var applicationName = Assembly.GetExecutingAssembly ().GetName ().Name;
			var applicationDataDirectory = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);

			var thisApplicationDataDirectory = Path.Combine (applicationDataDirectory, applicationName);
			if (!Directory.Exists (thisApplicationDataDirectory))
				Directory.CreateDirectory (thisApplicationDataDirectory);

			return thisApplicationDataDirectory;
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
					.AsImplementedInterfaces ()
					.WithParameter (new NamedParameter ("baseUri", clientUri))
					.WithProperty ("Timeout", new TimeSpan (0, 10, 0));

			return containerBuilder.Build ();
		}

		protected static int? ServiceDaemonPort(string applicationDataDirectoryPath)
		{
			// Read port from file.
			var portFilePath = Path.Combine (applicationDataDirectoryPath, "port");

			// Check if file exists.
			if (!File.Exists (portFilePath))
				return null;

			// Check if port in use (i.e. serivce-daemon is running).
			var port = int.Parse (File.ReadAllText (portFilePath));
			if (SocketHelpers.IsTCPPortAvailable (port))
				return null;

			return port;
		}

		protected static void SpawnServiceDaemon(string applicationDataDirectoryPath)
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