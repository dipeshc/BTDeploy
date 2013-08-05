using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading;
using MonoTorrent.Client;
using MonoTorrent.BEncoding;
using MonoTorrent.Client.Encryption;
using MonoTorrent.Dht.Listeners;
using MonoTorrent.Dht;
using MonoTorrent.Common;
using System.Collections.Generic;

namespace BTDeploy.ServiceDaemon.TorrentClients
{
	public class MonoTorrentClient : ITorrentClient
	{
		public int Port = 55999;
		public int DefaultTorrentUploadSlots = 4;
		public int DefaultTorrentOpenConnections = 150;

		protected readonly string DHTNodeFile;
		protected readonly string FastResumeFile;
		protected ClientEngine Engine;
		protected TorrentSettings DefaultTorrentSettings;
		protected BEncodedDictionary FastResume;

		public MonoTorrentClient(string applicationDataDirectoryPath)
		{
			var monoTorrentClientApplicationDataDirectoryPath = Path.Combine (applicationDataDirectoryPath);
			if (!Directory.Exists (monoTorrentClientApplicationDataDirectoryPath))
				Directory.CreateDirectory (monoTorrentClientApplicationDataDirectoryPath);

			DHTNodeFile = Path.Combine (monoTorrentClientApplicationDataDirectoryPath, "dhtNodes");
			FastResumeFile = Path.Combine (monoTorrentClientApplicationDataDirectoryPath, "fastResume");
		}

		public void Start()
		{
			// Create an instance of the engine.
			Engine = new ClientEngine (new EngineSettings
			{
				PreferEncryption = false,
				AllowedEncryption = EncryptionTypes.All
			});
			Engine.ChangeListenEndpoint (new IPEndPoint (IPAddress.Any, Port));

			// Make default torrent settings.
			DefaultTorrentSettings = new TorrentSettings (DefaultTorrentUploadSlots, DefaultTorrentOpenConnections, 0, 0);

			// Setup DHT listener.
			byte[] nodes = null;
			try
			{
				nodes = File.ReadAllBytes(DHTNodeFile);
			}
			catch
			{
			}
			var dhtListner = new DhtListener (new IPEndPoint (IPAddress.Any, Port));
			Engine.RegisterDht (new DhtEngine (dhtListner));
			dhtListner.Start ();
			Engine.DhtEngine.Start (nodes);

			// Fast resume.
			try
			{
				FastResume = BEncodedValue.Decode<BEncodedDictionary>(File.ReadAllBytes(FastResumeFile));
			}
			catch
			{
				FastResume = new BEncodedDictionary();
			}
		}

		public ITorrentDetails[] List ()
		{
			return Engine.Torrents.Select (Convert).ToArray();
		}

		public string Add (string torrentPath, string outputDirectoryPath)
		{
			// Create output directory.
			if (!Directory.Exists (outputDirectoryPath))
				Directory.CreateDirectory (outputDirectoryPath);

			// Make torrent and manager.
			var torrent = Torrent.Load(torrentPath);
			var torrentManager = new TorrentManager(torrent, outputDirectoryPath, DefaultTorrentSettings);

			// Check if already added.
			var existing = Engine.Torrents.FirstOrDefault (tm => tm.InfoHash.ToString () == torrentManager.InfoHash.ToString ());
			if (existing != null)
				return torrentManager.InfoHash.ToString ();

			// Setup fast resume.
			if (FastResume.ContainsKey (torrent.InfoHash.ToHex ()))
				torrentManager.LoadFastResume (new FastResume ((BEncodedDictionary)FastResume [torrent.InfoHash.ToHex ()]));

			// Register and start.
			Engine.Register(torrentManager);
			torrentManager.Start ();

			// Return id.
			return torrentManager.InfoHash.ToString();
		}

		public void Remove (string Id, bool deleteFiles = false)
		{
			var torrentManager = Engine.Torrents.First(tm => tm.InfoHash.ToString() == Id);

			torrentManager.Stop();
			Engine.Unregister(torrentManager);
			torrentManager.Dispose();

			if(deleteFiles)
				Directory.Delete(torrentManager.SavePath, true);
		}

		private TorrentDetails Convert(TorrentManager torrentManager)
		{
			var torrent = torrentManager.Torrent;
			var torrentDetails = new TorrentDetails
			{
				Id = torrent.InfoHash.ToString(),
				Name = torrent.Name,
				Files = torrent.Files.Select(f => f.Path).ToArray(),
				OutputDirectory =  torrentManager.SavePath,
				Size = torrent.Size,
				Progress = torrentManager.Progress,
				DownloadBytesPerSecond =  torrentManager.Monitor.DownloadSpeed,
				UploadBytesPerSecond = torrentManager.Monitor.UploadSpeed
			};
			switch(torrentManager.State)
			{
				case TorrentState.Hashing:
					torrentDetails.Status = TorrentStatus.Hashing;
					break;
				case TorrentState.Downloading:
				case TorrentState.Stopping:
				case TorrentState.Metadata:
					torrentDetails.Status = TorrentStatus.Downloading;
					break;
				case TorrentState.Seeding:
					torrentDetails.Status = TorrentStatus.Seeding;
					break;
				case TorrentState.Stopped:
				case TorrentState.Paused:
					torrentDetails.Status = TorrentStatus.Stopped;
					break;
				case TorrentState.Error:
					torrentDetails.Status = TorrentStatus.Error;
					break;
			}
			return torrentDetails;
		}
	}
}