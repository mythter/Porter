using System;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;

using Myth.Avalonia.Controls;
using Myth.Avalonia.Controls.Enums;
using Myth.Avalonia.Services.Abstractions;

using Porter.Models;
using Porter.Services;
using Porter.Services.Interfaces;

namespace Porter.Views;

public partial class MainWindow : Window
{
	#region Private Fields

	private bool _isClosing = false;

	private readonly IAppDataProvider<AppData> _appDataProvider;

	#endregion

	#region Public Properties

	public TrayIcon? TrayIcon { get; set; }

	public MiniWindow? MiniWindow { get; set; }

	#endregion

	#region Constructors

	public MainWindow(IAppDataProvider<AppData> appDataProvider)
	{
		_appDataProvider = appDataProvider;

		InitializeComponent();

		RestoreWindowSettings();

		PropertyChanged += (sender, e) =>
		{
			if (e.Property == IsVisibleProperty &&
				!IsVisible && !(MiniWindow?.IsVisible ?? false)
				&& TrayIcon is not null
				&& !_isClosing)
			{
				TrayIcon.IsVisible = true;
			}
		};
	}

	#endregion

	#region Overrides

	protected override async void OnOpened(EventArgs e)
	{
		base.OnOpened(e);

		TrayIcon?.IsVisible = false;

		await ShowCrashInfoIfExists();
	}

	protected override void OnClosing(WindowClosingEventArgs e)
	{
		base.OnClosing(e);

		SaveWindowSettings();

		if (!_appDataProvider.Value.Settings.OnCloseMinimizeToTray)
		{
			_isClosing = true;
			MiniWindow?.Close();
			return;
		}

		e.Cancel = true;
		Hide();

		TrayIcon?.IsVisible = true;
	}

	#endregion

	#region Private Methods

	private void SaveWindowSettings()
	{
		var windowSettings = _appDataProvider.Value.WindowSettings;

		windowSettings.Left = Position.X;
		windowSettings.Top = Position.Y;
		windowSettings.Width = Width;
		windowSettings.Height = Height;
		windowSettings.Maximized = WindowState == WindowState.Maximized;

		_appDataProvider.Save();
	}

	private void RestoreWindowSettings()
	{
		var windowSettings = _appDataProvider.Value.WindowSettings;

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

		if (DataContext is not IDialogContext context)
			return;

		await context.ShowMessageBoxDialog(
			"The application has been restarted due to a critical error. " +
			"For details, see the crash.log file in the application folder.",
			"Error occurred",
			icon: MessageBoxIcon.Error);

		CrashService.RemoveCrashData();
	}

	#endregion
}
