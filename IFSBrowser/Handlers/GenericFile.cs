using System.Xml.Linq;

namespace IFSBrowser.Handlers {

	public class GenericFile : Node {
		private int _start;

		protected internal GenericFile(IFS.FileBlob ifsData, XDocument manifest, Node parent = null, string path = "",
			string name = "") : base(ifsData, manifest, parent, path, name) { }

		public byte[] Data { get; internal set; }
		private int Size { get; set; }

		public void FromXml() {
			var manifest = Manifest;
			if (manifest.Root == null) return;

			var info = manifest.Root.Value.Split(" ");

			_start = int.Parse(info[0]);
			Size = int.Parse(info[1]);
			if (info.Length > 2) { }

			Load();
		}

		protected virtual void Load() {
			if (!CanRead()) return;

			Data = IFSData.Read(_start, Size);
		}

		public virtual bool CanRead() {
			return true;
		}
	}

}