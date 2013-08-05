using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace BTDeploy.Helpers
{
	public class ListFile<T> : List<T>
	{
		public readonly string FilePath;

		public ListFile()
		{
		}

		public ListFile(string filePath)
		{
			FilePath = filePath;
		}

		public void Load()
		{
			using (var reader = new StreamReader(FilePath))
			{
				var items = (ListFile<T>) new XmlSerializer (typeof(ListFile<T>)).Deserialize (reader);
				this.Clear ();
				this.AddRange (items);
			}
		}

		public void Save()
		{
			using (var writer = new StreamWriter(FilePath))
				new XmlSerializer (typeof(ListFile<T>)).Serialize (writer, this);
		}
	}
}