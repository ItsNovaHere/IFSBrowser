using System;
using System.Diagnostics;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace IFSBrowser {

	public class Window {
		private const int Centered = 0x2FFF0000;

		private static readonly WindowCreateInfo WindowCreateInfo =
			new WindowCreateInfo(Centered, Centered, 1280, 720, WindowState.Normal, "IFSBrowser");

		private static CommandList _commandList;
		private static GraphicsDevice _graphicsDevice;
		private static Framebuffer _framebuffer;
		private static Sdl2Window _window;
		private bool _resized;

		public Window() {
			VeldridStartup.CreateWindowAndGraphicsDevice(WindowCreateInfo, out _window, out _graphicsDevice);
			ImGuiRenderer = new ImGuiRenderer(_graphicsDevice, _graphicsDevice.SwapchainFramebuffer.OutputDescription,
				_window.Width, _window.Height);
			_commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
			_framebuffer = _graphicsDevice.SwapchainFramebuffer;

			_window.Resized += () => _resized = true;
		}

		public static int Width => _window.Width;
		public static int Height => _window.Height;
		public static GraphicsDevice GraphicsDevice => _graphicsDevice;
		public static ImGuiRenderer ImGuiRenderer { get; private set; }

		public event Action Draw;

		private static void WindowOnResized() {
			_graphicsDevice.MainSwapchain.Resize((uint) Width, (uint) Height);
			ImGuiRenderer.WindowResized(Width, Height);
			_framebuffer = _graphicsDevice.SwapchainFramebuffer;
		}

		private void Update(float deltaSeconds) {
			if (!_window.Exists) return;

			if (_resized) {
				_resized = false;

				WindowOnResized();
			}

			var input = _window.PumpEvents();
			ImGuiRenderer.Update(deltaSeconds, input);

			_commandList.Begin();
			_commandList.SetFramebuffer(_framebuffer);
			_commandList.ClearColorTarget(0, RgbaFloat.Black);

			Draw?.Invoke();

			ImGuiRenderer.Render(_graphicsDevice, _commandList);

			_commandList.End();

			_graphicsDevice.SubmitCommands(_commandList);
			_graphicsDevice.SwapBuffers();
		}

		public void Start() {
			long previousFrameTicks = 0;

			var sw = new Stopwatch();
			sw.Start();

			while (_window.Exists) {
				var currentFrameTicks = sw.ElapsedTicks;
				var deltaSeconds = (currentFrameTicks - previousFrameTicks) / (double) Stopwatch.Frequency;
				previousFrameTicks = currentFrameTicks;

				Update((float) deltaSeconds);
			}
		}
	}

}