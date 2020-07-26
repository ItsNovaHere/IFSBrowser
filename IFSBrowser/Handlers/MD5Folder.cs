using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml.Linq;
using KBinXML;

namespace IFSBrowser.Handlers {

	public class MD5Folder : GenericFolder {
		private readonly string _extension;
		private readonly string _md5Tag;
		private readonly bool _removeUnsolved;
		protected XMLFile InfoFile;

		protected MD5Folder(IFS.FileBlob ifsData, XDocument manifest, Node parent = null, string path = "", string name = "",
			string md5Tag = "", string extension = "", bool removeUnsolved = false) :
			base(ifsData, manifest, parent, path, name) {
			_md5Tag = md5Tag ?? name;
			_extension = extension;
			_removeUnsolved = removeUnsolved;
		}

		public override void FromXml() {
			base.FromXml();

			foreach (var (filename, file) in Files) {
				if (!filename.EndsWith(".xml")) continue;

				InfoFile = file as XMLFile;
				break;
			}

			ApplyMD5();
		}

		private void ApplyMD5() {
			var infoDoc = XDocument.Parse(InfoFile.XMLData);
			var md5 = MD5.Create();

			if (infoDoc.Root != null) {
				foreach (var name in infoDoc.Root.Descendants()
					.Where(x => x.Name.ToString() == _md5Tag)
					.Select(x => x.Attribute("name")?.Value)) {
					if (name == null) continue;

					var newName = name;
					var hashed = md5.ComputeHash(KBinReader.Encodings[0].GetBytes(name))
						.Aggregate("", (s, b) => s + b.ToString("x2"));

					if (!string.IsNullOrEmpty(_extension)) newName += _extension;

					if (Files.ContainsKey(name)) { }

					if (!Files.Remove(hashed, out var original)) continue;

					original.Name = newName;
					Files.Add(newName, original);
				}
			}

			// Sort files after fixing names
			Files = new Dictionary<string, GenericFile>(Files
				.Where(x => _removeUnsolved && x.Value.Name.Contains(".") || !_removeUnsolved)
				.OrderBy(x => x.Key));
		}
	}

}