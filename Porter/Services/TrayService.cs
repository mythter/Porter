using System;
using System.IO;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

using Porter.Enums;
using Porter.Interfaces;
using Porter.Views;

namespace Porter.Services
{
	public class TrayService : ITrayService
	{
		private const string IMAGES_PATH = "avares://Porter/Assets/Images";

		private ForwardState _iconState = ForwardState.None;

		public TrayIcon TrayIcon { get; }

		public TrayService(Application current, EventHandler exitAction, MainWindow mainWindow)
		{
			TrayIcon = InitTrayIcon(current, exitAction, mainWindow);
		}

		public void SetTrayIcon(ForwardState forwardState)
		{
			_iconState = forwardState;

			if (TrayIcon.IsVisible)
			{
				TrayIcon.Icon = GetWindowIcon(forwardState);
				TrayIcon.ToolTipText = GetTrayToolTip(forwardState);
			}
		}

		private TrayIcon InitTrayIcon(Application current, EventHandler exitAction, MainWindow mainWindow)
		{
			var exitItem = new NativeMenuItem("Exit");
			exitItem.Click += exitAction;

			var openMainWindowItem = new NativeMenuItem("Open Main Window");
			openMainWindowItem.Click += (s, e) =>
			{
				if (!mainWindow.IsVisible)
				{
					mainWindow.Show();
				}

				mainWindow.Activate();
			};

			var trayIcon = new TrayIcon
			{
				Icon = GetWindowIcon(ForwardState.None),
				ToolTipText = GetTrayToolTip(ForwardState.None),
				IsVisible = false,
				Menu = [
					openMainWindowItem,
					new NativeMenuItemSeparator(),
					exitItem,
				]
			};

			trayIcon.Clicked += (s, e) => mainWindow.MiniWindow?.ToggleVisibility();

			trayIcon.PropertyChanged += (s, e) =>
			{
				if (e.Property == TrayIcon.IsVisibleProperty && trayIcon.IsVisible)
				{
					SetTrayIcon(_iconState);
				}
			};

			var icons = new TrayIcons() { trayIcon };

			TrayIcon.SetIcons(current, icons);

			return trayIcon;
		}
		private static WindowIcon GetWindowIcon(ForwardState forwardState)
		{
			var icon = forwardState switch
			{
				ForwardState.AllUp => "logo-green.png",
				ForwardState.PartiallyDown => "logo-yellow.png",
				ForwardState.AllDown => "logo-red.png",
				_ => "logo-main.png"
			};

			return GetWindowIcon(Path.Combine(IMAGES_PATH, icon));
		}

		private static WindowIcon GetWindowIcon(string iconPath)
		{
			return new WindowIcon(new Bitmap(AssetLoader.Open(new Uri(iconPath))));
		}

		private static string GetTrayToolTip(ForwardState forwardState)
		{
			return forwardState switch
			{
				ForwardState.AllUp => "All Tunnels are started",
				ForwardState.PartiallyDown => "Some Tunnels are stopped",
				ForwardState.AllDown => "All Tunnels are stopped",
				_ => "Porter"
			};
		}
	}
}
