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
		public static void Main (string[] args)
		{
			// Make environment details.
			var environmentDetails = new EnvironmentDetails ();

			// Run as service deamon otherwise as client commands.
			if (args.Any () && args.First () == environmentDetails.ServiceDaemonCommand)
				RunServiceDaemon (environmentDetails);
			else
				RunClientCommand (environmentDetails, args);
		}

		protected static void RunServiceDaemon(IEnvironmentDetails environmentDetails)
		{
			// Make the torrent client.
			var monotTorrentClient = new MonoTorrentClient (environmentDetails.ApplicationDataDirectoryPath);
			monotTorrentClient.Start ();

			// Make torrent service app host.
			var servicesAppHost = new ServicesAppHost (environmentDetails, monotTorrentClient);
			servicesAppHost.Init ();

			// Never die.
			Thread.Sleep (Timeout.Infinite);
		}

		public static void RunClientCommand(IEnvironmentDetails environmentDetails, string[] args)
		{
			// Make the container.
			var containerBuilder = new ContainerBuilder();
			containerBuilder.RegisterAssemblyTypes (typeof(Program).Assembly)
					.Where (t => t.IsAssignableTo<ConsoleCommand> ())
					.As<ConsoleCommand> ()
					.OwnedByLifetimeScope();
			containerBuilder.RegisterType<EnvironmentDetails> ()
					.AsImplementedInterfaces ()
					.OwnedByLifetimeScope ();
			containerBuilder.RegisterType<JsonServiceClient> ()
					.WithParameter (new NamedParameter ("baseUri", environmentDetails.ServiceDaemonEndpoint))
					.WithProperty ("Timeout", new TimeSpan (0, 10, 0))
					.AsImplementedInterfaces ()
					.OwnedByLifetimeScope ();
			var container = containerBuilder.Build ();

			// Get the commands and run them.
			var commands = container.Resolve<IEnumerable<ConsoleCommand>> ().OrderBy (c => c.Command);
			ConsoleCommandDispatcher.DispatchCommand (commands, args, Console.Out);
		}
	}
}