using System;
using System.Linq;
using System.Numerics;
using System.Xml.Linq;
using Veldrid;

namespace IFSBrowser.Handlers {

	public class ImageFile : GenericFile {
		private string _compression;
		private string _format;
		private Texture _texture;

		public Vector2 ImageSize;

		private ImageFile(Node file) : base(file.IFSData, file.Manifest, file.Parent, file.Path, file.Name) { }

		public IntPtr Texture { get; private set; }

		public static ImageFile FromGeneric(GenericFile file, XElement image, string format, string compression) {
			var imageFile = new ImageFile(file) {
				_format = format,
				_compression = compression,
				Data = file.Data // reminder that i hate reading python
			};

			var imgRect = image.Element("imgrect")?.Value
				.Split(' ')
				.Select(x => int.Parse(x) / 2)
				.ToArray();

			if (imgRect != null) {
				imageFile.ImageSize = new Vector2(imgRect[1] - imgRect[0], imgRect[3] - imgRect[2]);
			}

			imageFile.Load();
			return imageFile;
		}

		protected override void Load() {
			if (_compression != "avslz") return;

			// LINQ .Reverse() is slow and messy but I'm too lazy to make it faster
			var rawSize = BitConverter.ToUInt32(Data[..4].Reverse().ToArray());
			var size = BitConverter.ToUInt32(Data[4..8].Reverse().ToArray());

			if (Data.Length != size + 8) return;

			Data = Data[8..];
			Data = LZ77.Decompress(Data);
			if (Data.Length != rawSize) Program.LogWarning("Size does not match.");
		}

		public override bool CanRead() {
			return _format == "argb8888rev";
		}


		private void GenerateArgb8888Rev(out TextureDescription textureDescription, out byte[] data) {
			textureDescription = new TextureDescription {
				ArrayLayers = 1,
				Depth = 1,
				Format = PixelFormat.B8_G8_R8_A8_UNorm,
				Height = (uint) ImageSize.Y,
				MipLevels = 1,
				SampleCount = TextureSampleCount.Count1,
				Type = TextureType.Texture2D,
				Usage = TextureUsage.Sampled,
				Width = (uint) ImageSize.X
			};

			data = Data;
		}

		public void GenerateTexture() {
			var factory = Window.GraphicsDevice.ResourceFactory;
			var gd = Window.GraphicsDevice;
			var imgui = Window.ImGuiRenderer;
			TextureDescription textureDefinition;
			byte[] data;

			if (_format == "argb8888rev") {
				GenerateArgb8888Rev(out textureDefinition, out data);
			} else {
				return;
			}

			_texture = factory.CreateTexture(ref textureDefinition);
			gd.UpdateTexture(_texture, data, 0, 0, 0, (uint) ImageSize.X, (uint) ImageSize.Y, 1, 0, 0);

			Texture = imgui.GetOrCreateImGuiBinding(factory, _texture);
		}


		public override void Dispose() {
			Window.ImGuiRenderer.RemoveImGuiBinding(_texture);

			_texture.Dispose();
		}
	}

}