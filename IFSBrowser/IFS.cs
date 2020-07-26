using System;
using System.IO;
using System.Xml.Linq;
using IFSBrowser.Handlers;
using KBinXML;

namespace IFSBrowser {

	public static class IFS {
		private const uint Signature = 0x6CAD8F89;

		public static GenericFolder Open(string path) {
			var file = File.OpenRead(path);
			var header = new ByteBuffer(file.Read(36));

			if (header.GetU32() != Signature) {
				Program.LogWarning("File is not an IFS file.");
				return null;
			}

			var version = header.GetU16();
			if ((header.GetU16() ^ version) != 0xFFFF) {
				Program.LogWarning("File version check failed.");
				return null;
			}

			header.GetU32(); // time
			header.GetU32(); // ifs size
			var manifestEnd = header.GetU32();
			var dataBlob = new FileBlob(file, manifestEnd);

			if (version > 1) header.Offset += 16;

			file.Seek(header.Offset, SeekOrigin.Begin);
			var manifest = new XDocument();
			try {
				manifest = new KBinReader(file.Read((int) (manifestEnd - header.Offset))).Document;
			} catch (Exception ex) {
				Program.LogWarning($"Error during manifest decoding: {ex}");
			}

			var root = new GenericFolder(dataBlob, manifest, null, path, Path.GetFileName(path));
			try {
				root.FromXml();
			} catch (Exception ex) {
				Program.LogWarning("Exception during ifs unpack: {0}", ex);
			}

			return root;
		}

		private static byte[] Read(this Stream file, int size) {
			var data = new byte[size];
			file.Read(data);
			return data;
		}

		public class FileBlob {
			private readonly FileStream _file;
			private readonly uint _offset;

			public FileBlob(FileStream fileStream, uint offset) {
				_file = fileStream;
				_offset = offset;
			}

			public byte[] Read(int offset, int size) {
				_file.Seek(offset + _offset, SeekOrigin.Begin);
				return _file.Read(size);
			}
		}
	}

}