using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace IFSBrowser.Handlers {

	public class GenericFolder : Node {
		private readonly Dictionary<string, GenericFolder> _folders = new Dictionary<string, GenericFolder>();

		protected Dictionary<string, GenericFile> Files = new Dictionary<string, GenericFile>();

		public GenericFolder(IFS.FileBlob ifsData, XDocument manifest, Node parent = null, string path = "", string name = "") :
			base(ifsData, manifest, parent, path, name) { }

		public virtual void FromXml() {
			var manifest = Manifest;
			if (manifest.Root == null) return;

			if (manifest.Root.Nodes().OfType<XText>().ToArray().Length > 0) { }

			foreach (var child in manifest.Root.Elements()) {
				var filename = FixName(child.Name.ToString());
				switch (filename) {
					case "_info_":
						continue;
					case "_super_": { }
						// TODO: subreference
						break;
					default: {
						if ((child.HasElements || child.Value.Split(" ").Length == 1) && child.Elements().First().Name != "i") {
							_folders.Add(filename,
								child.Name.ToString() == "tex"
									? new TextureFolder(IFSData, new XDocument(child), this, Path, filename)
									: new GenericFolder(IFSData, new XDocument(child), this, Path, filename));
						} else {
							Files.Add(filename,
								filename.EndsWith(".xml")
									? new XMLFile(IFSData, new XDocument(child), this, Path, filename)
									: new GenericFile(IFSData, new XDocument(child), this, Path, filename));
						}

						break;
					}
				}
			}

			foreach (var file in Files) file.Value.FromXml();

			foreach (var folder in _folders) folder.Value.FromXml();
		}

		public Dictionary<string, GenericFolder> GetFolders() {
			return _folders;
		}

		public Dictionary<string, GenericFile> GetFiles() {
			return Files;
		}

		public override void Dispose() {
			foreach (var file in Files) {
				file.Value.Dispose();
			}

			foreach (var folder in _folders) {
				folder.Value.Dispose();
			}

			GC.Collect();
		}
	}

}