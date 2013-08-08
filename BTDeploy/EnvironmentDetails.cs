using System;
using System.IO;
using System.Reflection;
using BTDeploy.Helpers;

namespace BTDeploy
{
	public class EnvironmentDetails : IEnvironmentDetails
	{
		public string ServiceDaemonCommand { get; set; }
		public string ApplicationDataDirectoryPath { get; set; }
		public int ServiceDaemonPort { get; set; }
		public string ServiceDaemonEndpoint { get; set; }

		public EnvironmentDetails()
		{
			ServiceDaemonCommand = "service-daemon";
			ApplicationDataDirectoryPath = MakeApplicationDataDirectoryPath ();
			ServiceDaemonPort = 10000;
			ServiceDaemonEndpoint = string.Format ("http://localhost:{0}/", ServiceDaemonPort);
		}

		private string MakeApplicationDataDirectoryPath()
		{
			// Make the path.
			var systemApplicationDataDirectory = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			var applicationDataDirectoryPath = Path.Combine (systemApplicationDataDirectory, Assembly.GetExecutingAssembly ().GetName ().Name);

			// Create the directory if it doesn't exist.
			if (!Directory.Exists (applicationDataDirectoryPath))
				Directory.CreateDirectory (applicationDataDirectoryPath);

			return applicationDataDirectoryPath;
		}
	}
}