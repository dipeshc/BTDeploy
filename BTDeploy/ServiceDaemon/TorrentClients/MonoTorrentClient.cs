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
using BTDeploy.Helpers;

namespace BTDeploy.ServiceDaemon.TorrentClients
{
	public class MonoTorrentClient : ITorrentClient
	{
		public int Port = 55999;
		public int DefaultTorrentUploadSlots = 4;
		public int DefaultTorrentOpenConnections = 150;

		protected readonly string TorrentFileDirectory;
		protected readonly string DHTNodeFile;
		protected readonly string FastResumeFile;
		protected readonly string TorrentMappingsCacheFile;
		protected ClientEngine Engine;
		protected TorrentSettings DefaultTorrentSettings;
		protected BEncodedDictionary FastResume;
		protected ListFile<TorrentMapping> TorrentMappingsCache;

		public MonoTorrentClient(string applicationDataDirectoryPath)
		{
			// Make directories.
			var monoTorrentClientApplicationDataDirectoryPath = Path.Combine (applicationDataDirectoryPath, this.GetType().Name);
			if (!Directory.Exists (monoTorrentClientApplicationDataDirectoryPath))
				Directory.CreateDirectory (monoTorrentClientApplicationDataDirectoryPath);

			TorrentFileDirectory = Path.Combine (monoTorrentClientApplicationDataDirectoryPath, "torrents");
			if (!Directory.Exists (TorrentFileDirectory))
				Directory.CreateDirectory (TorrentFileDirectory);

			// Make files.
			DHTNodeFile = Path.Combine (monoTorrentClientApplicationDataDirectoryPath, "dhtNodes");
			FastResumeFile = Path.Combine (monoTorrentClientApplicationDataDirectoryPath, "fastResume");
			TorrentMappingsCacheFile = Path.Combine (monoTorrentClientApplicationDataDirectoryPath, "torrentMappingsCache");

			// Make mappings cache.
			TorrentMappingsCache = new ListFile<TorrentMapping> (TorrentMappingsCacheFile);

			// Make default torrent settings.
			DefaultTorrentSettings = new TorrentSettings (DefaultTorrentUploadSlots, DefaultTorrentOpenConnections, 0, 0);
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

			// Try load the cache file.
			try
			{
				TorrentMappingsCache.Load();
			}
			catch
			{
			}

			// Cross reference torrent files against cache entries (sync).
			var torrents = Directory.GetFiles (TorrentFileDirectory, "*.torrent").Select (Torrent.Load).ToList();
			TorrentMappingsCache.RemoveAll (tmc => !torrents.Any (t => t.InfoHash.ToString () == tmc.InfoHash));
			TorrentMappingsCache.Save ();
			torrents.Where (t => !TorrentMappingsCache.Any (tmc => tmc.InfoHash == t.InfoHash.ToString ()))
					.ToList ().ForEach (t => File.Delete(t.TorrentPath));

			// Reload the torrents and add them.
			Directory.GetFiles (TorrentFileDirectory, "*.torrent").Select (Torrent.Load).ToList ().ForEach (torrent =>
			{
				var outputDirectoryPath = TorrentMappingsCache.First(tmc => tmc.InfoHash == torrent.InfoHash.ToString()).OutputDirectoryPath;
				Add(torrent, outputDirectoryPath);
			});
		}

		public ITorrentDetails[] List ()
		{
			return Engine.Torrents.Select (Convert).ToArray();
		}

		public string Add (Stream torrentFile, string outputDirectoryPath)
		{
			// Save torrent file.
			var applicationDataTorrentFilePath = Path.Combine (TorrentFileDirectory, Path.GetFileName(Path.GetTempFileName()) + ".torrent");
			using (var file = File.OpenWrite(applicationDataTorrentFilePath))
				StreamHelpers.CopyStream (torrentFile, file);

			// Create output directory.
			if (!Directory.Exists (outputDirectoryPath))
				Directory.CreateDirectory (outputDirectoryPath);

			// Load the torrent.
			var torrent = Torrent.Load (applicationDataTorrentFilePath);

			// Finally add.
			return Add (torrent, outputDirectoryPath);
		}

		protected string Add(Torrent torrent, string outputDirectoryPath)
		{
			// Create the torrent manager.
			var torrentManager = new TorrentManager(torrent, outputDirectoryPath, DefaultTorrentSettings, "");

			// Setup fast resume.
			if (FastResume.ContainsKey (torrent.InfoHash.ToHex ()))
				torrentManager.LoadFastResume (new FastResume ((BEncodedDictionary)FastResume [torrent.InfoHash.ToHex ()]));

			// Add to mappings cache.
			TorrentMappingsCache.RemoveAll (tmc => tmc.InfoHash == torrent.InfoHash.ToString ());
			TorrentMappingsCache.Add(new TorrentMapping
			{
				InfoHash = torrent.InfoHash.ToString(),
				OutputDirectoryPath = outputDirectoryPath
			});
			TorrentMappingsCache.Save ();

			// Register and start.
			Engine.Register(torrentManager);
			torrentManager.Start ();

			// Return Id.
			return torrentManager.InfoHash.ToString();
		}

		public void Remove (string Id, bool deleteFiles = false)
		{
			// Get the torrent manager.
			var torrentManager = Engine.Torrents.First(tm => tm.InfoHash.ToString() == Id);

			// Delete the torrent file.
			File.Delete (torrentManager.Torrent.TorrentPath);

			// Delete the cache reference.
			TorrentMappingsCache.RemoveAll (tmc => tmc.InfoHash == torrentManager.Torrent.InfoHash.ToString ());
			TorrentMappingsCache.Save ();

			// Stop and remove the torrent from the engine.
			torrentManager.Stop();
			Engine.Unregister(torrentManager);
			torrentManager.Dispose();

			// Delete files if required.
			if(deleteFiles)
				Directory.Delete(torrentManager.SavePath, true);
		}

		public Stream Create (string fileSourceDirectory)
		{
			// Create source.
			var source = new TorrentFileSource (fileSourceDirectory, true);

			// Make creator.
			var creator = new TorrentCreator ();
			creator.PieceLength = TorrentCreator.RecommendedPieceSize (source.Files);

			// Make torrent and return.
			var torrent = new MemoryStream ();
			creator.Create (source, torrent);
			return torrent;
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

		public class TorrentMapping
		{
			public string InfoHash { get; set; }
			public string OutputDirectoryPath { get; set; }

			public TorrentMapping() {}
		}
	}
}