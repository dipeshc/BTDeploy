using System;
using System.IO;

namespace BTDeploy.ServiceDaemon.TorrentClients
{
	public interface ITorrentClient
	{
		ITorrentDetails[] List();
		string Add(string torrentFilePath, string outputDirectoryPath);
		void Remove(string Id, bool deleteFiles = false);
		Stream Create(string fileSourceDirectory);
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