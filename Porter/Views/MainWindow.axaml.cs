using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;

using Porter.Interfaces;
using Porter.Services;
using Porter.Storage;

namespace Porter.Views;

public partial class MainWindow : Window
{
	private bool _isClosing = false;

	public TrayIcon? TrayIcon { get; set; }

	public MiniWindow? MiniWindow { get; set; }

	public IDialogService? DialogService { get; set; }

	public MainWindow()
	{
		InitializeComponent();

		RestoreWindowSettings();

		PropertyChanged += (sender, e) =>
		{
			if (e.Property == IsVisibleProperty &&
				!IsVisible && !(MiniWindow?.IsVisible ?? true)
				&& TrayIcon is not null
				&& !_isClosing)
			{
				TrayIcon.IsVisible = true;
			}
		};
	}

	protected override async void OnOpened(EventArgs e)
	{
		base.OnOpened(e);

		if (TrayIcon is not null)
			TrayIcon.IsVisible = false;

		await ShowCrashInfoIfExists();
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		base.OnClosing(e);

		SaveWindowSettings();

		if (!StorageManager.Settings.OnCloseMinimizeToTray)
		{
			_isClosing = true;
			MiniWindow?.Close();
			return;
		}

		e.Cancel = true;
		Hide();

		if (TrayIcon is not null)
			TrayIcon.IsVisible = true;
	}

	private void SaveWindowSettings()
	{
		var windowSettings = StorageManager.WindowSettings;

		windowSettings.Left = Position.X;
		windowSettings.Top = Position.Y;
		windowSettings.Width = Width;
		windowSettings.Height = Height;
		windowSettings.Maximized = WindowState == WindowState.Maximized;

		StorageManager.SaveWindowSettings(windowSettings);
	}

	private void RestoreWindowSettings()
	{
		var windowSettings = StorageManager.WindowSettings;

		if (windowSettings.Maximized)
		{
			WindowState = WindowState.Maximized;
			return;
		}

		var screen = Screens.ScreenFromPoint(new PixelPoint(windowSettings.Left, windowSettings.Top));
		if (screen is null)
		{
			WindowStartupLocation = WindowStartupLocation.CenterScreen;
		}
		else if (windowSettings.Width > 0 && windowSettings.Height > 0)
		{
			Position = new PixelPoint(windowSettings.Left, windowSettings.Top);
			Width = windowSettings.Width;
			Height = windowSettings.Height;
		}
	}

	private async Task ShowCrashInfoIfExists()
	{
		if (CrashService.GetCrashData() is null)
		{
			return;
		}
		else
		{
			CrashService.RemoveCrashData();
		}

		if (DialogService is null)
			return;

		await DialogService.ShowErrorAsync(
			"The application has been restarted due to a critical error." +
			"For details, see the crash.log file in the application folder.",
			"Error occurred");

	}
}
