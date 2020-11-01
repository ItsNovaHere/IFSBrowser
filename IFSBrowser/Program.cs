using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using IFSBrowser.Handlers;
using ImGuiNET;
using static ImGuiNET.ImGui;
using static IFSBrowser.ImGui;

namespace IFSBrowser {

	internal static class Program {
		private static Window _window;

		private static bool _openFile;
		private static float _frameHeight;
		private static float _imageZoom = 1f;
		private static List<GenericFolder> _ifsFiles;
		private static GenericFile _openedFile;
		private static ImGuiStylePtr _style;

		private static void Main(string[] args) {
			LogInformation("IFSBrowser v{0}", Assembly.GetExecutingAssembly().GetName().Version?.ToString(3));
			_window = new Window();
			_window.Draw += Draw;

			_style = GetStyle();
			_style.WindowRounding = 0f;
			_style.Colors[(int) ImGuiCol.FrameBg] = _style.Colors[(int) ImGuiCol.WindowBg];
			_style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
			_style.FrameBorderSize = 1f;

			_ifsFiles = new List<GenericFolder>();

			if (args.Length >= 1) {
				foreach (var arg in args) {
					try {
						OpenIFS(arg);
					} catch (Exception ex) {
						LogWarning("Exception while opening file from args: {0}", ex);
					}
				}
			}

			_window.Start();
		}

		private static void Draw() {
			SetNextWindowSize(new Vector2(Window.Width, Window.Height), ImGuiCond.Always);
			SetNextWindowPos(Vector2.Zero, ImGuiCond.Always);

			Begin("IFSBrowser",
				ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoTitleBar |
					ImGuiWindowFlags.MenuBar);

			// Menu Bar
			if (BeginMenuBar()) {
				if (BeginMenu("File")) {
					if (MenuItem("Open...", "Ctrl+O")) _openFile = true;

					EndMenu();
				}

				_frameHeight = GetWindowHeight() - GetFrameHeight() - _style.WindowPadding.Y * 2;

				EndMenuBar();
			}

			// File Select
			if (_openFile) {
				if (FileSelect(ref _openFile, out var openPath, ".ifs")) {
					_openFile = false;
					OpenIFS(openPath);
				}
			}

			// Main UI
			if (_ifsFiles != null) {
				Columns(2, "", true);

				BeginChildFrame(0x010, new Vector2(GetColumnWidth() - _style.FramePadding.X * 4, _frameHeight));
				DrawIFSTree(_ifsFiles);
				EndChildFrame();

				NextColumn();

				BeginChildFrame(0x100, new Vector2(GetColumnWidth() - _style.FramePadding.X * 4, _frameHeight),
					ImGuiWindowFlags.HorizontalScrollbar);
				DrawOpenFile();

				EndChildFrame();
			}

			End();
		}

		private static void DrawOpenFile() {
			if (_openedFile == null) return;

			switch (Path.GetExtension(_openedFile.Name)) {
				case ".xml" when _openedFile is XMLFile xmlFile:
					TextUnformatted(xmlFile.XMLData);
					break;
				case ".png" when _openedFile is ImageFile imageFile:
					if (imageFile.Texture == IntPtr.Zero) imageFile.GenerateTexture();

					SliderFloat("Zoom", ref _imageZoom, 0.5f, 10f);
					if (IsItemHovered() && IsMouseDown(1)) _imageZoom = 1f; // reset

					BeginChildFrame(0x1000,
						new Vector2(GetColumnWidth(),
							_frameHeight - GetTextLineHeightWithSpacing() - _style.FramePadding.Y * 4),
						ImGuiWindowFlags.HorizontalScrollbar);
					Image(imageFile.Texture, imageFile.ImageSize * _imageZoom, Vector2.Zero, Vector2.One);
					EndChildFrame();
					break;
			}
		}

		private static void DrawIFSTree(IEnumerable<GenericFolder> roots) {
			// ToList ensures closing files doesn't throw exceptions, although rip performance
			foreach (var root in roots.ToList()) DrawIFSTree(root, true);
		}

		private static void DrawIFSTree(GenericFolder root, bool isFile = false) {
			var open = CollapsingHeader(root.Name);

			if (isFile && BeginPopupContextItem()) {
				if (MenuItem("Close")) {
					_ifsFiles.Remove(root);

					if (_openedFile.Path == root.Path) {
						_openedFile = null;
					}
					
					root.Dispose();
					GC.Collect();
				}

				EndPopup();
			}

			if (!open) return;

			Indent(16);
			PushID(root.Name);

			foreach (var (_, folder) in root.GetFolders()) DrawIFSTree(folder);

			foreach (var (name, file) in root.GetFiles()) {
				if (file.CanRead()) {
					if (Selectable(name)) _openedFile = file;
				} else {
					Text(name);
				}
			}

			PopID();
			Unindent(16);
		}

		private static void OpenIFS(string openPath) {
			LogInformation("Opening {0}", openPath);

			var openedFile = IFS.Open(openPath);
			_ifsFiles.Add(openedFile);

			LogInformation("Opened {0}", openedFile.Name);
			LogInformation("Contains {0} folders and {1} files.", openedFile.GetFolders().Count,
				openedFile.GetFiles().Count);
		}

		private static void LogInformation(string format, params object[] args) {
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine($"[INFO | {DateTime.Now:T}] {format}", args);
			Console.ResetColor();
		}

		internal static void LogWarning(string format, params object[] args) {
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine($"[WARN | {DateTime.Now:T}] {format}", args);
			Console.ResetColor();
		}
	}

}