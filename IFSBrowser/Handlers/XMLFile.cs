using System;
using System.Text;
using System.Xml.Linq;
using KBinXML;

namespace IFSBrowser.Handlers {

	public class XMLFile : GenericFile {
		protected internal XMLFile(IFS.FileBlob ifsData, XDocument manifest, Node parent = null, string path = "",
			string name = "") : base(ifsData, manifest, parent, path, name) { }

		public string XMLData { get; private set; }

		protected override void Load() {
			base.Load();

			try {
				Data = Encoding.UTF8.GetBytes(XMLData = new KBinReader(Data).Document.ToString());
			} catch (Exception ex) {
				Program.LogWarning("Exception while decoding {0}: {1}", Name, ex);
			}
		}

		public override bool CanRead() {
			return true;
		}
	}

}