using System.Xml.Linq;

namespace IFSBrowser.Handlers {

	public class TextureFolder : MD5Folder {
		public TextureFolder(IFS.FileBlob ifsData, XDocument manifest, Node parent = null, string path = "", string name = "") :
			base(ifsData, manifest, parent, path, name, "image", ".png") { }

		public override void FromXml() {
			base.FromXml();

			CreateImages(XDocument.Parse(InfoFile.XMLData).Root?.Attribute("compress")?.Value);
		}

		private void CreateImages(string compression) {
			var elements = XDocument.Parse(InfoFile.XMLData).Root?.Elements();
			if (elements == null) return;

			foreach (var canvas in elements) {
				var format = canvas.Attribute("format")?.Value;

				foreach (var texture in canvas.Elements()) {
					if (texture.Name.ToString() != "image") continue;

					var name = $"{texture.Attribute("name")?.Value}.png";

					if (Files.Remove(name, out var file)) {
						Files.Add(name, ImageFile.FromGeneric(file, texture, format, compression));
					}
				}
			}
		}
	}

}