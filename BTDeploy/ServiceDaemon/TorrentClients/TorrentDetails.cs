namespace BTDeploy.ServiceDaemon.TorrentClients
{
	public class TorrentDetails : ITorrentDetails
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public string[] Files { get; set; }
		public string OutputDirectory { get; set; }
		public TorrentStatus Status { get; set; }
		public long Size { get; set; }
		public double Progress { get; set; }
		public double DownloadBytesPerSecond { get; set; }
		public double UploadBytesPerSecond { get; set; }
	}
}