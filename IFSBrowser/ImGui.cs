using System.IO;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using static ImGuiNET.ImGui;

namespace IFSBrowser {

	public static class ImGui {
		private static ImGuiStylePtr _style;

		static ImGui() {
			// init arrays
			FileSelectChangeFolder(_fileSelectPath, "");

			_style = GetStyle();
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

			if (_fileSelectPath.Split(Path.DirectorySeparatorChar).Length > 1 && Selectable("..")) {
				FileSelectChangeFolder(Path.Join(_fileSelectPath.Split(Path.DirectorySeparatorChar)[..^1]), filter);
			}

			foreach (var directory in _fileSelectFolders) {
				if (Selectable(directory)) {
					FileSelectChangeFolder($"{_fileSelectPath}{Path.DirectorySeparatorChar}{directory}", filter);
				}
			}

			Separator();

			foreach (var file in _fileSelectFiles) {
				if (file.Contains(_fileSelectSearch) && Selectable(file)) {
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

			_fileSelectFolders = Directory.EnumerateDirectories(_fileSelectPath + Path.DirectorySeparatorChar)
				.Select(Path.GetFileName)
				.ToArray();
		}
	}

}