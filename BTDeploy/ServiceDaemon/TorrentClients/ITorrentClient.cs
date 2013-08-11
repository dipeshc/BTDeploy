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
		Stream Create(string name, string sourceDirectoryPath, IEnumerable<string> Trackers = null);
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

	public class TorrentAlreadyAddedException : ITorrentClientException
	{
		public TorrentAlreadyAddedException(string message = "", Exception innerException = null) : base (message, innerException) { }
	}

	public class InvalidOutputDirectoryException : ITorrentClientException
	{
		public InvalidOutputDirectoryException(string message = "", Exception innerException = null) : base (message, innerException) { }
	}

	public class OutputDirectoryAlreadyInUseException : ITorrentClientException
	{
		public OutputDirectoryAlreadyInUseException(string message = "", Exception innerException = null) : base (message, innerException) { }
	}

	public class InvalidSourceDirectoryException : ITorrentClientException
	{
		public InvalidSourceDirectoryException(string message = "", Exception innerException = null) : base (message, innerException) { }
	}

	public abstract class ITorrentClientException : Exception
	{
		public ITorrentClientException(string message = "", Exception innerException = null) : base (message, innerException) { }
	}
}