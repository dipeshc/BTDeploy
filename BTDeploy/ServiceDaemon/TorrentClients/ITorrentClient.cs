using System;
using System.IO;
using System.Collections.Generic;

namespace BTDeploy.ServiceDaemon.TorrentClients
{
	public interface ITorrentClient
	{
		ITorrentDetails[] List();
		string Add(Stream torrentFile, string outputDirectoryPath);
		void Remove(string Id, bool deleteFiles = false);
		Stream Create(string name, string fileSourceDirectory, IEnumerable<string> Trackers = null);
	}

	public interface ITorrentDetails
	{
		string Id { get; }
		string Name { get; }
		string[] Files { get; }
		string OutputDirectory { get; }
		TorrentStatus Status { get; }
		long Size { get; }
		double Progress { get; }
		double DownloadBytesPerSecond { get; }
		double UploadBytesPerSecond { get; }
	}

	public enum TorrentStatus
	{
		Hashing,
		Downloading,
		Seeding,
		Stopped,
		Error
	}
}