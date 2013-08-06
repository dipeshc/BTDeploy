using System;
using System.IO;

namespace BTDeploy.Helpers
{
	public static class FileSystemHelpers
	{
		public static void DeleteEmptyDirectory(string directoryPath)
		{
			foreach (var directory in Directory.GetDirectories(directoryPath))
			{
				DeleteEmptyDirectory (directory);
				if (Directory.GetFiles (directory).Length == 0 && Directory.GetDirectories (directory).Length == 0)
					Directory.Delete (directory, false);
			}
		}
	}
}