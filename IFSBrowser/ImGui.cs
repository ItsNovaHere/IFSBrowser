using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using static ImGuiNET.ImGui;

namespace IFSBrowser {

	public static class ImGui {
		private static ImGuiStylePtr _style;

		private const string Folder = "\uF07B ";
		private const string File = "\uF15B ";
		private const string FileArchive = "\uF1C6 ";
		private const string Disk = "\uF0A0 ";
		private static readonly ushort[] IconCharRange = {0xE005, 0xF8FF, 0}; 
		
		static ImGui() {
			// init arrays
			FileSelectChangeFolder(_fileSelectPath, "");

			_style = GetStyle();
			
			// Font Icon Loading
			unsafe {
				var rangeHandle = GCHandle.Alloc(IconCharRange, GCHandleType.Pinned);
				var nativeConfig = ImGuiNative.ImFontConfig_ImFontConfig();
				nativeConfig->MergeMode = 1;
				nativeConfig->GlyphMinAdvanceX = 13.0f;

				try {
					GetIO().Fonts.AddFontFromFileTTF("fa-regular-400.ttf", 16.0f, nativeConfig,
						rangeHandle.AddrOfPinnedObject());
				} finally {
					if (rangeHandle.IsAllocated) {
						rangeHandle.Free();
					}
				}
				
				ImGuiNative.ImFontConfig_destroy(nativeConfig);
			}

			Window.ImGuiRenderer.RecreateFontDeviceTexture();
		}

		private static string[] _fileSelectFiles;
		private static string[] _fileSelectFolders;
		private static string _fileSelectSearch = "";

		private static string _fileSelectPath = System.Environment.CurrentDirectory;


		public static bool FileSelect(ref bool open, out string path, string filter = "") {
			path = "";

			if (!Begin("File Select", ref open,
				ImGuiWindowFlags.Popup | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar)) {
				return false;
			}
			InputText("Search", ref _fileSelectSearch, 256);

			BeginChildFrame(0x10000, new Vector2(
				GetWindowWidth() - _style.WindowPadding.X * 2,
				GetWindowHeight() - GetTextLineHeightWithSpacing() - _style.WindowPadding.Y * 6));

			if (_fileSelectPath != "" && Selectable("..")) {
				FileSelectChangeFolder(Path.Join(_fileSelectPath.Split(Path.DirectorySeparatorChar)[..^1]), filter);
			}

			foreach (var directory in _fileSelectFolders) {
				// loose check for disks
				var icon = directory.EndsWith(":") ? Disk : Folder;
				if (Selectable(icon + directory)) {
					FileSelectChangeFolder(Path.Combine(_fileSelectPath, directory), filter);
				}
			}

			Separator();

			foreach (var file in _fileSelectFiles) {
				// loose check for ifs icon
				var icon = file.EndsWith(".ifs") ? FileArchive : File;
				if (file.Contains(_fileSelectSearch) && Selectable(icon + file)) {
					path = Path.Join(_fileSelectPath, file);
				}
			}

			EndChildFrame();

			EndPopup();

			return path != "";
		}

		private static void FileSelectChangeFolder(string newPath, string filter) {
			_fileSelectPath = newPath;
			
			_fileSelectFiles = Directory.EnumerateFiles(_fileSelectPath + Path.DirectorySeparatorChar)
            					.Where(x => filter == "" || x.EndsWith(filter))
            					.Select(Path.GetFileName)
            					.ToArray();

			if (_fileSelectPath != "") {
				_fileSelectFolders = Directory.EnumerateDirectories(_fileSelectPath + Path.DirectorySeparatorChar)
					.Select(Path.GetFileName)
					.ToArray();
				return;
			}
			
			// Drive Select
			_fileSelectFolders = Directory.GetLogicalDrives()
				.Select(x => x.TrimEnd('\\'))
				.ToArray();
		}
	}

}